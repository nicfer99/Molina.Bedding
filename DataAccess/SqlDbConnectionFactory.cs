using Microsoft.Data.SqlClient;

namespace Molina.Bedding.Mvc.DataAccess;

public class SqlDbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlDbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("U_MOLINA")
            ?? throw new InvalidOperationException("Connection string 'U_MOLINA' non trovata in appsettings.");
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
