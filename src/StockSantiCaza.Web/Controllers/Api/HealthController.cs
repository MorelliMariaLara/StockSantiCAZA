using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Data;

namespace StockSantiCaza.Web.Controllers.Api;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;
    private readonly IConfiguration configuration;

    public HealthController(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IConfiguration configuration)
    {
        this.dbContextFactory = dbContextFactory;
        this.configuration = configuration;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        var sqlServer = ExtraerValor(connectionString, "Server");
        var authMode = DetectarModoAuth(connectionString);

        return Ok(new
        {
            status = "ok",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            sqlServer,
            authMode,
            sqlUser = authMode == "sql" ? ExtraerValor(connectionString, "User Id", "UID") : null,
            tieneProductionJson = System.IO.File.Exists(
                Path.Combine(AppContext.BaseDirectory, "appsettings.Production.json"))
        });
    }

    [HttpGet("db")]
    public async Task<IActionResult> Database(CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(12));

        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(timeout.Token);
            var conexion = db.Database.GetDbConnection();
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
                mensaje = "La base SQL no respondió a tiempo. Revise appsettings.Production.json en el servidor (Server=sql2016, contraseña correcta)."
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

        return mensaje;
    }
}
