using Microsoft.Data.SqlClient;

namespace StockSantiCaza.Web.Configuration;

public static class ConnectionStringResolver
{
    public static string Resolve(IConfiguration configuration)
    {
        var plantilla = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not configured.");

        var sqlPassword = configuration["Database:SqlPassword"];
        if (string.IsNullOrWhiteSpace(sqlPassword))
        {
            return plantilla;
        }

        var builder = new SqlConnectionStringBuilder(plantilla)
        {
            Password = sqlPassword
        };

        return builder.ConnectionString;
    }
}
