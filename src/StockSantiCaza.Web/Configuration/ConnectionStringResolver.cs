using Microsoft.Data.SqlClient;

namespace StockSantiCaza.Web.Configuration;

public static class ConnectionStringResolver
{
    public static string Resolve(IConfiguration configuration, string? dataSource = null) =>
        CrearBuilder(configuration, dataSource, integrated: null).ConnectionString;

    public static SqlConnectionStringBuilder CrearBuilder(
        IConfiguration configuration,
        string? dataSource = null,
        bool? integrated = null)
    {
        var plantilla = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not configured.");

        var sqlPassword = configuration["Database:SqlPassword"];
        var plantillaLimpia = string.IsNullOrWhiteSpace(sqlPassword)
            ? plantilla
            : QuitarClave(plantilla, "Password");

        try
        {
            var builder = new SqlConnectionStringBuilder(plantillaLimpia);
            if (!string.IsNullOrWhiteSpace(sqlPassword))
            {
                builder.Password = sqlPassword;
            }

            if (integrated.HasValue)
            {
                builder.IntegratedSecurity = integrated.Value;
                if (integrated.Value)
                {
                    builder.UserID = string.Empty;
                    builder.Password = string.Empty;
                }
            }

            var servidor = dataSource
                ?? configuration["Database:DataSource"]?.Trim();
            if (!string.IsNullOrWhiteSpace(servidor))
            {
                builder.DataSource = servidor;
            }

            builder.MultipleActiveResultSets = false;
            builder.Encrypt = false;
            builder.TrustServerCertificate = true;
            return builder;
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException(
                "Cadena SQL inválida. Si la contraseña tiene '@', configurá Database.SqlPassword en appsettings.Production.json.",
                ex);
        }
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
