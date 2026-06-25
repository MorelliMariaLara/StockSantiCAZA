using Microsoft.Data.SqlClient;

namespace StockSantiCaza.Web.Data;

public static class DatabaseConnection
{
    public const string DefaultConnectionName = "DefaultConnection";
    public const string RecommendedServer = "LARA-NB\\SQLEXPRESS02";
    public const string RecommendedDatabase = "StockSantiCAZA";
    public const string RecommendedConnectionString =
        "Server=LARA-NB\\SQLEXPRESS02;Database=StockSantiCAZA;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False";

    public static string Resolve(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DefaultConnectionName)
            ?? RecommendedConnectionString;

        if (connectionString.Contains("(localdb)", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"La cadena de conexión apunta a LocalDB. Use la instancia {RecommendedServer}, por ejemplo: {RecommendedConnectionString}");
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
