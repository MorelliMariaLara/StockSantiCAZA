using Microsoft.Data.SqlClient;

namespace StockSantiCaza.Web.Data;

public static class DatabaseConnection
{
    public const string DefaultConnectionName = "DefaultConnection";

    public static string Resolve(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DefaultConnectionName)
            ?? throw new InvalidOperationException($"Connection string '{DefaultConnectionName}' was not configured.");

        if (connectionString.Contains("(localdb)", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "La cadena de conexión apunta a LocalDB. Para este proyecto use la instancia SQLEXPRESS02, por ejemplo: Server=.\\SQLEXPRESS02;Database=StockSantiCAZA;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True");
        }

        return connectionString;
    }

    public static string Describe(string connectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return $"{builder.DataSource} / {builder.InitialCatalog}";
        }
        catch
        {
            return "cadena de conexión no válida";
        }
    }
}
