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

        try
        {
            var builder = new SqlConnectionStringBuilder(plantillaLimpia);
            if (!string.IsNullOrWhiteSpace(sqlPassword))
            {
                builder.Password = sqlPassword;
            }

            var dataSource = configuration["Database:DataSource"];
            if (!string.IsNullOrWhiteSpace(dataSource))
            {
                builder.DataSource = dataSource.Trim();
            }

            builder.MultipleActiveResultSets = false;
            builder.Encrypt = false;
            builder.TrustServerCertificate = true;
            return builder.ConnectionString;
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException(
                "Cadena SQL inválida. Si la contraseña tiene '@', configurá Database.SqlPassword en appsettings.Production.json.",
                ex);
        }
    }

    public static async Task<(bool ok, string? servidor, string? mensaje, int? sqlError)> ProbarConexionAsync(
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var builder = new SqlConnectionStringBuilder(Resolve(configuration))
        {
            ConnectTimeout = 8
        };

        try
        {
            await using var conexion = new SqlConnection(builder.ConnectionString);
            await conexion.OpenAsync(cancellationToken);
            return (true, builder.DataSource, null, null);
        }
        catch (SqlException ex)
        {
            return (false, builder.DataSource, ex.Message, ex.Number);
        }
        catch (Exception ex)
        {
            return (false, builder.DataSource, ex.Message, null);
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
