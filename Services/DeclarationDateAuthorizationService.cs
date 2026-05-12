using Microsoft.Data.SqlClient;
using Molina.Bedding.Mvc.DataAccess;

namespace Molina.Bedding.Mvc.Services;

public class DeclarationDateAuthorizationService : IDeclarationDateAuthorizationService
{
    private const string BaseQuery = @"
SELECT
    prg_operatore_bedding,
    num_level,
    sig_pin_password
FROM dbo.X_OE_OPERATORI_BEDDING
WHERE ISNULL(bol_annullato, 0) = 0";

    private readonly IDbConnectionFactory _dbConnectionFactory;

    public DeclarationDateAuthorizationService(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public bool CanAnySelectedOperatorEditDate(IEnumerable<int> operatorIds)
    {
        var authorizedOperators = LoadAuthorizedOperators();
        return authorizedOperators.Count > 0;
    }

    public DeclarationDateAuthorizationResult AuthorizeForDateEdit(IEnumerable<int> operatorIds, string? pin)
    {
        var normalizedPin = (pin ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedPin))
        {
            return new DeclarationDateAuthorizationResult
            {
                Success = false,
                Message = "Inserisci il PIN per modificare la data."
            };
        }

        var matchingOperator = LoadAuthorizedOperators()
            .FirstOrDefault(entry => string.Equals(entry.Pin, normalizedPin, StringComparison.Ordinal));

        if (matchingOperator is null)
        {
            return new DeclarationDateAuthorizationResult
            {
                Success = false,
                Message = "PIN non valido o operatore non abilitato alla modifica data."
            };
        }

        return new DeclarationDateAuthorizationResult
        {
            Success = true,
            Level = matchingOperator.Level,
            Message = "PIN corretto. Ora puoi modificare la data."
        };
    }

    private IReadOnlyList<DeclarationDateAuthorizedOperator> LoadAuthorizedOperators()
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = BaseQuery
            + " AND ISNULL(num_level, 0) = 1"
            + " AND NULLIF(LTRIM(RTRIM(ISNULL(sig_pin_password, ''))), '') IS NOT NULL"
            + " ORDER BY prg_operatore_bedding";

        connection.Open();

        using var reader = command.ExecuteReader();
        var authorizedOperators = new List<DeclarationDateAuthorizedOperator>();
        while (reader.Read())
        {
            authorizedOperators.Add(new DeclarationDateAuthorizedOperator
            {
                OperatorId = reader.GetInt32(reader.GetOrdinal("prg_operatore_bedding")),
                Level = reader["num_level"] == DBNull.Value ? 0 : Convert.ToInt32(reader["num_level"]),
                Pin = (reader["sig_pin_password"] == DBNull.Value ? string.Empty : Convert.ToString(reader["sig_pin_password"]))?.Trim() ?? string.Empty
            });
        }

        return authorizedOperators;
    }

    private sealed class DeclarationDateAuthorizedOperator
    {
        public int OperatorId { get; init; }
        public int Level { get; init; }
        public string Pin { get; init; } = string.Empty;
    }
}
