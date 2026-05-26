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
        return GetAll();
    }

    public IReadOnlyList<DeclarationNoteTypeViewModel> GetForProductionDeclarations()
    {
        return GetAll();
    }

    private IReadOnlyList<DeclarationNoteTypeViewModel> GetAll()
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                prg_dichiarazione_tipo_nota,
                des_dichiarazione_tipo_nota,
                ISNULL(bol_testo_annotazioni, 0) AS bol_testo_annotazioni
            FROM dbo.X_OE_PROD_DICH_TIPI_NOTE
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
}
