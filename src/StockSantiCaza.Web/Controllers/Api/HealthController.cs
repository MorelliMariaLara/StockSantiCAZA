using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StockSantiCaza.Web.Configuration;

namespace StockSantiCaza.Web.Controllers.Api;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IConfiguration configuration;

    public HealthController(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var connectionString = ConnectionStringResolver.Resolve(configuration);
        var builder = new SqlConnectionStringBuilder(connectionString);
        var authMode = builder.IntegratedSecurity ? "integrated" : "sql";

        return Ok(new
        {
            status = "ok",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            os = RuntimeInformation.OSDescription,
            isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            sqlServer = builder.DataSource,
            database = builder.InitialCatalog,
            authMode,
            sqlUser = authMode == "sql" ? builder.UserID : null,
            tieneSqlPassword = ConnectionStringResolver.TieneSqlPassword(configuration),
            tieneProductionJson = System.IO.File.Exists(
                Path.Combine(AppContext.BaseDirectory, "appsettings.Production.json")),
            usaVariableEntorno = !string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"))
        });
    }

    [HttpGet("db")]
    public async Task<IActionResult> Database(CancellationToken ct)
    {
        var connectionString = ConnectionStringResolver.Resolve(configuration);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return StatusCode(503, new
            {
                status = "error",
                database = "error",
                mensaje = "No hay cadena DefaultConnection configurada."
            });
        }

        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            ConnectTimeout = 15
        };

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(18));

        try
        {
            await Task.Run(async () =>
            {
                await using var conexion = new SqlConnection(builder.ConnectionString);
                await conexion.OpenAsync(timeout.Token);
            }, timeout.Token);

            return Ok(new { status = "ok", database = "connected" });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(503, new
            {
                status = "error",
                database = "timeout",
                mensaje = "La base SQL no respondió a tiempo. Use User Id w400048_MariAdmin + Database.SqlPassword en appsettings.Production.json (Integrated Security da 502 en Ferozo)."
            });
        }
        catch (SqlException ex)
        {
            return StatusCode(503, new
            {
                status = "error",
                database = "error",
                sqlError = ex.Number,
                mensaje = SanitizarMensajeSql(ex.Message)
            });
        }
        catch (Exception ex)
        {
            var mensaje = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(503, new
            {
                status = "error",
                database = "error",
                mensaje = SanitizarMensajeSql(mensaje)
            });
        }
    }

    private static string SanitizarMensajeSql(string mensaje)
    {
        if (mensaje.Contains("Login failed", StringComparison.OrdinalIgnoreCase))
        {
            return "Login failed: usuario o contraseña SQL incorrectos en appsettings.Production.json.";
        }

        if (mensaje.Contains("Cannot open database", StringComparison.OrdinalIgnoreCase))
        {
            return "No se puede abrir la base w400048_santicazarmeria. Verifique que el usuario tenga permisos en el panel Ferozo.";
        }

        if (mensaje.Contains("network path was not found", StringComparison.OrdinalIgnoreCase)
            || mensaje.Contains("network-related", StringComparison.OrdinalIgnoreCase)
            || mensaje.Contains("server was not found", StringComparison.OrdinalIgnoreCase))
        {
            return "No se encuentra sql2016 desde el servidor web. En Ferozo use tcp:sql2016,1433. sql2016 no funciona desde su PC en local, solo en el hosting publicado.";
        }

        if (mensaje.Contains("Integrated", StringComparison.OrdinalIgnoreCase)
            && mensaje.Contains("not supported", StringComparison.OrdinalIgnoreCase))
        {
            return "Integrated Security no funciona en este servidor. Use User Id + Password con Integrated Security=False.";
        }

        if (mensaje.Contains("Keyword not supported", StringComparison.OrdinalIgnoreCase))
        {
            return "Contraseña SQL mal configurada (carácter @). Use Database.SqlPassword en appsettings.Production.json y quite Password= de la cadena y de web.config en el servidor.";
        }

        return mensaje;
    }
}
