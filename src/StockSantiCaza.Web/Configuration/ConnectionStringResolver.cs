using Microsoft.Data.SqlClient;

namespace StockSantiCaza.Web.Configuration;

public static class ConnectionStringResolver
{
    public static string Resolve(IConfiguration configuration)
    {
        var plantilla = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not configured.");

        var sqlPassword = configuration["Database:SqlPassword"];
        var plantillaLimpia = string.IsNullOrWhiteSpace(sqlPassword)
            ? plantilla
            : QuitarClave(plantilla, "Password");

        plantillaLimpia = QuitarClave(plantillaLimpia, "proveedorName");
        plantillaLimpia = QuitarClave(plantillaLimpia, "providerName");

        try
        {
            var builder = new SqlConnectionStringBuilder(plantillaLimpia);
            if (!string.IsNullOrWhiteSpace(sqlPassword))
            {
                builder.Password = sqlPassword;
            }

            builder.MultipleActiveResultSets = false;
            builder.Encrypt = false;
            builder.TrustServerCertificate = true;
            return builder.ConnectionString;
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException(
                "Cadena SQL inválida. Si la contraseña tiene '@', usá Database.SqlPassword en appsettings.Production.json.",
                ex);
        }
    }

    public static SqlConnectionStringBuilder CrearBuilder(
        IConfiguration configuration,
        string dataSource,
        bool integrated)
    {
        var builder = new SqlConnectionStringBuilder(Resolve(configuration))
        {
            DataSource = dataSource,
            IntegratedSecurity = integrated
        };

        if (integrated)
        {
            builder.UserID = string.Empty;
            builder.Password = string.Empty;
        }

        return builder;
    }

    public static bool TieneSqlPassword(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration["Database:SqlPassword"]);

    private static string QuitarClave(string connectionString, string clave)
    {
        var partes = connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(parte => !parte.StartsWith(clave + "=", StringComparison.OrdinalIgnoreCase));

        return string.Join(';', partes);
    }
}
