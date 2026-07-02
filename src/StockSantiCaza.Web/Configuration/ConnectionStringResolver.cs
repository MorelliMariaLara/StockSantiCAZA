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

            builder.MultipleActiveResultSets = false;
            AplicarFormatoFerozo(builder);
            return builder.ConnectionString;
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException(
                "Cadena SQL inválida. Si la contraseña tiene '@', configurá Database.SqlPassword en appsettings.Production.json " +
                "y quitá Password= de la cadena y de variables en web.config.",
                ex);
        }
    }

    public static bool TieneSqlPassword(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration["Database:SqlPassword"]);

    private static void AplicarFormatoFerozo(SqlConnectionStringBuilder builder)
    {
        // Integrated Security en Ferozo: usar sql2016 sin prefijo tcp (tcp cuelga o tumba el worker).
        if (builder.IntegratedSecurity)
        {
            return;
        }

        var servidor = builder.DataSource.Trim();
        if (servidor.Equals("sql2016", StringComparison.OrdinalIgnoreCase))
        {
            builder.DataSource = "tcp:sql2016,1433";
        }
    }

    private static string QuitarClave(string connectionString, string clave)
    {
        var partes = connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(parte => !parte.StartsWith(clave + "=", StringComparison.OrdinalIgnoreCase));

        return string.Join(';', partes);
    }
}
