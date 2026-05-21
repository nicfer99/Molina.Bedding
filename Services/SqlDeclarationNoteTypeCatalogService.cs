using Microsoft.Data.SqlClient;
using Molina.Bedding.Mvc.DataAccess;
using Molina.Bedding.Mvc.Models;

namespace Molina.Bedding.Mvc.Services;

public class SqlDeclarationNoteTypeCatalogService : IDeclarationNoteTypeCatalogService
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public SqlDeclarationNoteTypeCatalogService(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public IReadOnlyList<DeclarationNoteTypeViewModel> GetForGenericDeclarations()
    {
        return GetByDeclarationKind(isGenericDeclaration: true);
    }

    public IReadOnlyList<DeclarationNoteTypeViewModel> GetForProductionDeclarations()
    {
        return GetByDeclarationKind(isGenericDeclaration: false);
    }

    private IReadOnlyList<DeclarationNoteTypeViewModel> GetByDeclarationKind(bool isGenericDeclaration)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        connection.Open();

        var hasGenericDeclarationColumn = HasGenericDeclarationColumn(connection);
        var genericDeclarationFilter = hasGenericDeclarationColumn
            ? isGenericDeclaration
                ? "WHERE ISNULL(bol_dichiarazione_generica, 0) = 1"
                : "WHERE ISNULL(bol_dichiarazione_generica, 0) = 0"
            : string.Empty;

        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
                prg_dichiarazione_tipo_nota,
                des_dichiarazione_tipo_nota,
                ISNULL(bol_testo_annotazioni, 0) AS bol_testo_annotazioni
            FROM dbo.X_OE_PROD_DICH_TIPI_NOTE
            {genericDeclarationFilter}
            ORDER BY des_dichiarazione_tipo_nota
            """;

        using var reader = command.ExecuteReader();
        var noteTypes = new List<DeclarationNoteTypeViewModel>();

        while (reader.Read())
        {
            noteTypes.Add(new DeclarationNoteTypeViewModel
            {
                Id = reader.GetInt32(reader.GetOrdinal("prg_dichiarazione_tipo_nota")),
                Description = reader.GetString(reader.GetOrdinal("des_dichiarazione_tipo_nota")),
                RequiresAnnotationText = Convert.ToBoolean(reader["bol_testo_annotazioni"])
            });
        }

        return noteTypes;
    }

    private static bool HasGenericDeclarationColumn(SqlConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT CASE WHEN COL_LENGTH('dbo.X_OE_PROD_DICH_TIPI_NOTE', 'bol_dichiarazione_generica') IS NULL THEN 0 ELSE 1 END";
        return Convert.ToInt32(command.ExecuteScalar()) == 1;
    }
}
