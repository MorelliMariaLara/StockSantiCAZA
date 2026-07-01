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
        var sqlServer = ExtraerSqlServer(connectionString);

        return Ok(new
        {
            status = "ok",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            sqlServer,
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
            var ok = await db.Database.CanConnectAsync(timeout.Token);
            return ok
                ? Ok(new { status = "ok", database = "connected" })
                : StatusCode(503, new { status = "error", database = "unreachable" });
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
            return StatusCode(503, new { status = "error", database = ex.Message });
        }
    }

    private static string ExtraerSqlServer(string connectionString)
    {
        var server = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim())
            .FirstOrDefault(part => part.StartsWith("Server=", StringComparison.OrdinalIgnoreCase));
        return server is null ? "?" : server["Server=".Length..];
    }
}
