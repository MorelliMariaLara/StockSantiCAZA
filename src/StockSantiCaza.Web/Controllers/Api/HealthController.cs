using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Data;

namespace StockSantiCaza.Web.Controllers.Api;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public HealthController(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _dbFactory = dbFactory;
        _configuration = configuration;
        _environment = environment;
    }

    /// <summary>
    /// Comprueba si el proceso .NET responde (no requiere base de datos).
    /// </summary>
    [HttpGet]
    public IActionResult Ping()
    {
        return Ok(new
        {
            status = "ok",
            app = "StockSantiCAZA",
            environment = _environment.EnvironmentName,
            timeUtc = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Comprueba conectividad SQL usando la cadena de appsettings.Production.json.
    /// </summary>
    [HttpGet("db")]
    public async Task<IActionResult> Database(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "error",
                database = "missing_connection_string",
                error = "No hay ConnectionStrings:DefaultConnection configurada."
            });
        }

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "error",
                    database = "unreachable",
                    error = "No se pudo conectar a SQL Server."
                });
            }

            var server = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .FirstOrDefault(part => part.StartsWith("Server=", StringComparison.OrdinalIgnoreCase))
                ?? "Server=?";

            var database = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .FirstOrDefault(part => part.StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                ?? "Database=?";

            return Ok(new
            {
                status = "ok",
                database = "connected",
                server,
                databaseName = database.Replace("Database=", string.Empty, StringComparison.OrdinalIgnoreCase)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "error",
                database = "exception",
                error = ex.Message
            });
        }
    }
}
