using Microsoft.Data.SqlClient;

namespace Molina.Bedding.Mvc.DataAccess;

public interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
}
