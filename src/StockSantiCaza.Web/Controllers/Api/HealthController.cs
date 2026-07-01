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
        var sqlServer = ExtraerValor(connectionString, "Server");
        var authMode = DetectarModoAuth(connectionString);

        return Ok(new
        {
            status = "ok",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            os = RuntimeInformation.OSDescription,
            isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            sqlServer,
            authMode,
            sqlUser = authMode == "sql" ? ExtraerValor(connectionString, "User Id", "UID", "User ID") : null,
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

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        try
        {
            await using var conexion = new SqlConnection(connectionString);
            await conexion.OpenAsync(timeout.Token);
            await conexion.CloseAsync();

            return Ok(new { status = "ok", database = "connected" });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(503, new
            {
                status = "error",
                database = "timeout",
                mensaje = "La base SQL no respondió a tiempo. En Ferozo el servidor sql2016 solo funciona desde el hosting Windows del mismo plan."
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

    private static string DetectarModoAuth(string connectionString)
    {
        var partes = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim());

        foreach (var parte in partes)
        {
            if (parte.StartsWith("Integrated Security=", StringComparison.OrdinalIgnoreCase)
                && parte.EndsWith("True", StringComparison.OrdinalIgnoreCase))
            {
                return "integrated";
            }

            if (parte.StartsWith("Trusted_Connection=", StringComparison.OrdinalIgnoreCase)
                && parte.EndsWith("True", StringComparison.OrdinalIgnoreCase))
            {
                return "integrated";
            }
        }

        return string.IsNullOrWhiteSpace(ExtraerValor(connectionString, "User Id", "UID"))
            ? "integrated"
            : "sql";
    }

    private static string ExtraerValor(string connectionString, params string[] claves)
    {
        foreach (var parte in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var texto = parte.Trim();
            foreach (var clave in claves)
            {
                var prefijo = clave + "=";
                if (texto.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
                {
                    return texto[prefijo.Length..];
                }
            }
        }

        return string.Empty;
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

        if (mensaje.Contains("network-related", StringComparison.OrdinalIgnoreCase)
            || mensaje.Contains("server was not found", StringComparison.OrdinalIgnoreCase))
        {
            return "No se encuentra el servidor sql2016 desde el hosting. Verifique que el plan sea Windows Hosting y que el sitio esté en la misma cuenta Ferozo que la base.";
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
