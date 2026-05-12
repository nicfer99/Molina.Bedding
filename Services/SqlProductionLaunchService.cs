using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Molina.Bedding.Mvc.DataAccess;
using Molina.Bedding.Mvc.Models;

namespace Molina.Bedding.Mvc.Services;

public class SqlProductionLaunchService : IProductionLaunchService
{
    private static readonly string[] MaterialLotCodeColumnCandidates =
    [
        "cod_lotto",
        "des_lotto",
        "lotto",
        "des_campo_libero2"
    ];

    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly IConfiguration _configuration;

    public SqlProductionLaunchService(IDbConnectionFactory dbConnectionFactory, IConfiguration configuration)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _configuration = configuration;
    }

    public IReadOnlyList<ProductionLaunchItemViewModel> GetOpenLaunches(string lineCode, bool resolveMaterialLots = false)
    {
        const string sql = """
            SELECT
                prg_ordine,
                des_campo_libero2,
                num_doc,
                qta_merce,
                qta_dichiarata,
                des_linea_produzione,
                cod_art
            FROM [dbo].[X_OE_VW_PROD_LANCIO]
            WHERE sig_serie_doc = 'PC'
              AND ind_stato_evas = 'I'
              AND cod_linea_produzione = @lineCode
            ORDER BY des_campo_libero2, prg_ordine
            """;

        using var connection = _dbConnectionFactory.CreateConnection();
        connection.Open();

        var items = LoadLaunches(connection, sql, [new SqlParameter("@lineCode", lineCode)]);
        if (resolveMaterialLots)
        {
            ApplyMaterialLots(connection, items);
        }

        return items;
    }

    public IReadOnlyList<ProductionLaunchItemViewModel> GetOpenLaunchesByOrderIds(string lineCode, IReadOnlyList<int> orderIds, bool resolveMaterialLots = false)
    {
        var normalizedIds = orderIds
            .Distinct()
            .OrderBy(static value => value)
            .ToList();

        if (normalizedIds.Count == 0)
        {
            return [];
        }

        var parameterNames = new List<string>();
        for (var index = 0; index < normalizedIds.Count; index++)
        {
            parameterNames.Add($"@orderId{index}");
        }

        var sql = $"""
            SELECT
                prg_ordine,
                des_campo_libero2,
                num_doc,
                qta_merce,
                qta_dichiarata,
                des_linea_produzione,
                cod_art
            FROM [dbo].[X_OE_VW_PROD_LANCIO]
            WHERE sig_serie_doc = 'PC'
              AND ind_stato_evas = 'I'
              AND cod_linea_produzione = @lineCode
              AND prg_ordine IN ({string.Join(", ", parameterNames)})
            ORDER BY des_campo_libero2, prg_ordine
            """;

        var parameters = new List<SqlParameter>
        {
            new("@lineCode", lineCode)
        };

        for (var index = 0; index < normalizedIds.Count; index++)
        {
            parameters.Add(new SqlParameter(parameterNames[index], normalizedIds[index]));
        }

        using var connection = _dbConnectionFactory.CreateConnection();
        connection.Open();

        var items = LoadLaunches(connection, sql, parameters);
        if (resolveMaterialLots)
        {
            ApplyMaterialLots(connection, items);
        }

        return items;
    }

    private static List<ProductionLaunchItemViewModel> LoadLaunches(SqlConnection connection, string sql, IReadOnlyList<SqlParameter> parameters)
    {
        var items = new List<ProductionLaunchItemViewModel>();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(MapLaunch(reader));
        }

        return items;
    }

    private void ApplyMaterialLots(SqlConnection connection, List<ProductionLaunchItemViewModel> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        foreach (var item in items)
        {
            var materialArticleCodes = LoadMaterialArticleCodes(connection, item.OrderId);
            if (materialArticleCodes.Count == 0)
            {
                item.MaterialLotValidationMessage = "Non ho trovato nessuna riga MP bedding riempimento.";
                item.AvailableMaterialLots = [];
                continue;
            }

            if (materialArticleCodes.Count > 1)
            {
                item.MaterialLotValidationMessage = "Ho trovato più righe MP bedding riempimento. Correggi i dati prima di proseguire.";
                item.AvailableMaterialLots = [];
                continue;
            }

            item.ArticleCode = materialArticleCodes[0];
        }

        var metadata = ResolveMaterialLotSourceMetadata(connection);
        if (metadata is null)
        {
            const string metadataError = "Non riesco a determinare in modo univoco la vista lotti da usare per l'articolo materiale.";
            foreach (var item in items.Where(static item => string.IsNullOrWhiteSpace(item.MaterialLotValidationMessage)))
            {
                item.MaterialLotValidationMessage = metadataError;
                item.AvailableMaterialLots = [];
            }

            return;
        }

        var articleCodes = items
            .Where(static item => string.IsNullOrWhiteSpace(item.MaterialLotValidationMessage))
            .Select(static item => item.ArticleCode)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value)
            .ToList();

        if (articleCodes.Count == 0)
        {
            return;
        }

        var lotsByArticleCode = LoadMaterialLotsByArticleCode(connection, metadata, articleCodes);
        foreach (var item in items.Where(static item => string.IsNullOrWhiteSpace(item.MaterialLotValidationMessage)))
        {
            if (string.IsNullOrWhiteSpace(item.ArticleCode) || !lotsByArticleCode.TryGetValue(item.ArticleCode, out var lots) || lots.Count == 0)
            {
                item.MaterialLotValidationMessage = $"Non ho trovato lotti disponibili nel deposito 002 per l'articolo {item.ArticleCode}.";
                item.AvailableMaterialLots = [];
                continue;
            }

            item.AvailableMaterialLots = lots;
        }
    }

    private static List<string> LoadMaterialArticleCodes(SqlConnection connection, int orderId)
    {
        const string sql = """
            SELECT cod_art
            FROM [dbo].[X_OE_VW_PROD_LANCIO_MP]
            WHERE ISNULL(bol_is_mp_bedding_riempimento, 0) = 1
              AND prg_qpr = @orderId
            ORDER BY cod_art
            """;

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@orderId", orderId));

        var result = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var articleCode = reader["cod_art"] == DBNull.Value
                ? string.Empty
                : Convert.ToString(reader["cod_art"])?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(articleCode))
            {
                continue;
            }

            if (!result.Contains(articleCode, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(articleCode);
            }
        }

        return result;
    }

    private MaterialLotSourceMetadata? ResolveMaterialLotSourceMetadata(SqlConnection connection)
    {
        var configuredViewName = (_configuration["MaterialLots:ViewName"] ?? string.Empty).Trim();
        var configuredLotCodeColumn = (_configuration["MaterialLots:LotCodeColumnName"] ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(configuredViewName) && !string.IsNullOrWhiteSpace(configuredLotCodeColumn))
        {
            return new MaterialLotSourceMetadata(configuredViewName, configuredLotCodeColumn);
        }

        return new MaterialLotSourceMetadata("X_OE_VW_LOTTI", "cod_lotto");
    }

    private static Dictionary<string, List<string>> LoadMaterialLotsByArticleCode(SqlConnection connection, MaterialLotSourceMetadata metadata, IReadOnlyList<string> articleCodes)
    {
        var normalizedArticleCodes = articleCodes
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value)
            .ToList();

        if (normalizedArticleCodes.Count == 0)
        {
            return [];
        }

        var parameterNames = new List<string>();
        for (var index = 0; index < normalizedArticleCodes.Count; index++)
        {
            parameterNames.Add($"@articleCode{index}");
        }

        var sql = $"""
            SELECT
                cod_art,
                CONVERT(varchar(255), {EscapeIdentifier(metadata.LotCodeColumnName)}) AS lot_code
            FROM [dbo].{EscapeIdentifier(metadata.ViewName)}
            WHERE cod_art IN ({string.Join(", ", parameterNames)})
              AND cod_dep = @depositCode
              AND ISNULL(qta_esistenza, 0) <> 0
              AND NULLIF(LTRIM(RTRIM(CONVERT(varchar(255), {EscapeIdentifier(metadata.LotCodeColumnName)}))), '') IS NOT NULL
            ORDER BY cod_art, CONVERT(varchar(255), {EscapeIdentifier(metadata.LotCodeColumnName)})
            """;

        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@depositCode", "002"));
        for (var index = 0; index < normalizedArticleCodes.Count; index++)
        {
            command.Parameters.Add(new SqlParameter(parameterNames[index], normalizedArticleCodes[index]));
        }

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var articleCode = reader["cod_art"] == DBNull.Value
                ? string.Empty
                : Convert.ToString(reader["cod_art"])?.Trim() ?? string.Empty;
            var lotCode = reader["lot_code"] == DBNull.Value
                ? string.Empty
                : Convert.ToString(reader["lot_code"])?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(articleCode) || string.IsNullOrWhiteSpace(lotCode))
            {
                continue;
            }

            if (!result.TryGetValue(articleCode, out var lots))
            {
                lots = [];
                result[articleCode] = lots;
            }

            if (!lots.Contains(lotCode, StringComparer.OrdinalIgnoreCase))
            {
                lots.Add(lotCode);
            }
        }

        return result;
    }

    private static string NormalizeLookupValue(string? value)
    {
        return (value ?? string.Empty).Trim();
    }

    private static string EscapeIdentifier(string identifier)
    {
        return $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
    }

    private static ProductionLaunchItemViewModel MapLaunch(SqlDataReader reader)
    {
        var lotCode = reader["des_campo_libero2"] == DBNull.Value
            ? string.Empty
            : Convert.ToString(reader["des_campo_libero2"]) ?? string.Empty;

        var documentNumber = reader["num_doc"] == DBNull.Value
            ? string.Empty
            : Convert.ToString(reader["num_doc"]) ?? string.Empty;

        var quantityToProduce = reader["qta_merce"] == DBNull.Value
            ? 0m
            : Convert.ToDecimal(reader["qta_merce"]);

        var quantityProduced = reader["qta_dichiarata"] == DBNull.Value
            ? 0m
            : Convert.ToDecimal(reader["qta_dichiarata"]);

        var lineDescription = reader["des_linea_produzione"] == DBNull.Value
            ? string.Empty
            : Convert.ToString(reader["des_linea_produzione"]) ?? string.Empty;

        var articleCode = reader["cod_art"] == DBNull.Value
            ? string.Empty
            : Convert.ToString(reader["cod_art"])?.Trim() ?? string.Empty;

        return new ProductionLaunchItemViewModel
        {
            OrderId = Convert.ToInt32(reader["prg_ordine"]),
            LotCode = lotCode,
            DocumentNumber = documentNumber,
            QuantityToProduce = quantityToProduce,
            QuantityProduced = quantityProduced,
            LineDescription = lineDescription,
            ArticleCode = articleCode
        };
    }

    private sealed record MaterialLotSourceMetadata(string ViewName, string LotCodeColumnName);
}
