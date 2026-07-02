using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StockSantiCaza.Web.Configuration;

namespace StockSantiCaza.Web.Controllers.Api;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private const string AppBuild = "ferozo-sql-004";

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
            appBuild = AppBuild,
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
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(12));

        try
        {
            var (ok, servidor, mensaje, sqlError) = await ConnectionStringResolver.ProbarConexionAsync(
                configuration,
                timeout.Token);

            if (ok)
            {
                return Ok(new { status = "ok", database = "connected", sqlServer = servidor, appBuild = AppBuild });
            }

            return StatusCode(503, new
            {
                status = "error",
                database = "error",
                appBuild = AppBuild,
                sqlServer = servidor,
                sqlError,
                mensaje
            });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(503, new
            {
                status = "error",
                database = "timeout",
                appBuild = AppBuild,
                mensaje = "La base no respondió en 12 segundos."
            });
        }
    }
}
