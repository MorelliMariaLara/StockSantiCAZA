using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace StockSantiCaza.Web.Configuration;

public static class ConnectionStringResolver
{
    private static string? dataSourceFerozoCache;

    private static readonly string[] CandidatosServidorFerozo =
    {
        "sql2016,1433",
        "tcp:sql2016,1433",
        "sql2016"
    };

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
            AplicarServidorFerozo(builder, configuration);
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

    public static async Task<string> ResolveProduccionAsync(
        IConfiguration configuration,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var dataSourceConfigurado = configuration["Database:DataSource"];
        if (!string.IsNullOrWhiteSpace(dataSourceConfigurado))
        {
            dataSourceFerozoCache = dataSourceConfigurado.Trim();
            logger?.LogInformation("[SQL] Usando Database:DataSource = {DataSource}", dataSourceFerozoCache);
            return Resolve(configuration);
        }

        if (!string.IsNullOrWhiteSpace(dataSourceFerozoCache))
        {
            return Resolve(configuration);
        }

        var baseBuilder = new SqlConnectionStringBuilder(Resolve(configuration));
        if (!EsServidorFerozo(baseBuilder.DataSource))
        {
            return baseBuilder.ConnectionString;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));

        var errores = new List<string>();
        foreach (var candidato in CandidatosServidorFerozo)
        {
            baseBuilder.DataSource = candidato;
            baseBuilder.ConnectTimeout = 8;

            try
            {
                await using var conexion = new SqlConnection(baseBuilder.ConnectionString);
                await conexion.OpenAsync(timeout.Token);
                dataSourceFerozoCache = candidato;
                logger?.LogInformation("[SQL] Servidor Ferozo detectado: {DataSource}", candidato);
                return baseBuilder.ConnectionString;
            }
            catch (Exception ex)
            {
                errores.Add($"{candidato}: {ex.Message}");
            }
        }

        logger?.LogWarning("[SQL] No se pudo abrir SQL en Ferozo. Intentos: {Errores}", string.Join(" | ", errores));
        return Resolve(configuration);
    }

    public static async Task<(bool ok, string? servidor, string? error)> ProbarConexionAsync(
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var dataSourceConfigurado = configuration["Database:DataSource"];
        var candidatos = string.IsNullOrWhiteSpace(dataSourceConfigurado)
            ? CandidatosServidorFerozo
            : new[] { dataSourceConfigurado.Trim() };

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));

        var errores = new List<object>();
        foreach (var candidato in candidatos)
        {
            var builder = new SqlConnectionStringBuilder(Resolve(configuration))
            {
                DataSource = candidato,
                ConnectTimeout = 8
            };

            try
            {
                await Task.Run(async () =>
                {
                    await using var conexion = new SqlConnection(builder.ConnectionString);
                    await conexion.OpenAsync(timeout.Token);
                }, timeout.Token);

                dataSourceFerozoCache = candidato;
                return (true, candidato, null);
            }
            catch (Exception ex)
            {
                errores.Add(new { servidor = candidato, mensaje = ex.Message });
            }
        }

        return (false, null, System.Text.Json.JsonSerializer.Serialize(errores));
    }

    public static bool TieneSqlPassword(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration["Database:SqlPassword"]);

    private static void AplicarServidorFerozo(SqlConnectionStringBuilder builder, IConfiguration configuration)
    {
        var dataSourceConfigurado = configuration["Database:DataSource"];
        if (!string.IsNullOrWhiteSpace(dataSourceConfigurado))
        {
            builder.DataSource = dataSourceConfigurado.Trim();
            return;
        }

        if (!string.IsNullOrWhiteSpace(dataSourceFerozoCache) && EsServidorFerozo(builder.DataSource))
        {
            builder.DataSource = dataSourceFerozoCache;
        }
    }

    private static bool EsServidorFerozo(string dataSource)
    {
        var servidor = dataSource.Trim();
        return servidor.Equals("sql2016", StringComparison.OrdinalIgnoreCase)
            || servidor.Equals("sql2016,1433", StringComparison.OrdinalIgnoreCase)
            || servidor.Equals("tcp:sql2016,1433", StringComparison.OrdinalIgnoreCase);
    }

    private static string QuitarClave(string connectionString, string clave)
    {
        var partes = connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(parte => !parte.StartsWith(clave + "=", StringComparison.OrdinalIgnoreCase));

        return string.Join(';', partes);
    }
}
