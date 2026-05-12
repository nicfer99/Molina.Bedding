using Microsoft.Data.SqlClient;
using Molina.Bedding.Mvc.DataAccess;
using Molina.Bedding.Mvc.Models;

namespace Molina.Bedding.Mvc.Services;

public class SqlOperatorCatalogService : IOperatorCatalogService
{
    private const string BaseQuery = @"
SELECT
    prg_operatore_bedding,
    des_operatore_bedding
FROM dbo.X_OE_OPERATORI_BEDDING
WHERE ISNULL(bol_annullato, 0) = 0";

    private readonly IDbConnectionFactory _dbConnectionFactory;

    public SqlOperatorCatalogService(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public IReadOnlyList<OperatorItemViewModel> GetAllActive()
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = BaseQuery + " ORDER BY des_operatore_bedding";

        connection.Open();

        using var reader = command.ExecuteReader();
        var operators = new List<OperatorItemViewModel>();

        while (reader.Read())
        {
            operators.Add(MapOperator(reader));
        }

        return operators;
    }

    public IReadOnlyList<OperatorItemViewModel> GetByIds(IEnumerable<int> ids)
    {
        var distinctIds = ids
            .Distinct()
            .OrderBy(static id => id)
            .ToList();

        if (distinctIds.Count == 0)
        {
            return [];
        }

        using var connection = _dbConnectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        var parameterNames = new List<string>(distinctIds.Count);
        for (var index = 0; index < distinctIds.Count; index++)
        {
            var parameterName = $"@id{index}";
            parameterNames.Add(parameterName);
            command.Parameters.Add(new SqlParameter(parameterName, distinctIds[index]));
        }

        command.CommandText = BaseQuery
            + $" AND prg_operatore_bedding IN ({string.Join(", ", parameterNames)})"
            + " ORDER BY des_operatore_bedding";

        connection.Open();

        using var reader = command.ExecuteReader();
        var operators = new List<OperatorItemViewModel>();

        while (reader.Read())
        {
            operators.Add(MapOperator(reader));
        }

        return operators;
    }

    private static OperatorItemViewModel MapOperator(SqlDataReader reader)
    {
        return new OperatorItemViewModel
        {
            Id = reader.GetInt32(reader.GetOrdinal("prg_operatore_bedding")),
            Name = reader.GetString(reader.GetOrdinal("des_operatore_bedding")),
            Kind = "Operatore"
        };
    }
}
