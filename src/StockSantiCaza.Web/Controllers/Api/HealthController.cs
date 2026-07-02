using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StockSantiCaza.Web.Configuration;

namespace StockSantiCaza.Web.Controllers.Api;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private const string AppBuild = "ferozo-sql-003";

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
        var (ok, servidor, error) = await ConnectionStringResolver.ProbarConexionAsync(configuration, ct);
        if (ok)
        {
            return Ok(new { status = "ok", database = "connected", sqlServer = servidor });
        }

        return StatusCode(503, new
        {
            status = "error",
            database = "error",
            mensaje = "No se pudo conectar a sql2016. Probá agregar Database:DataSource en appsettings.Production.json (valores: sql2016, sql2016,1433 o tcp:sql2016,1433).",
            intentos = error
        });
    }
}
