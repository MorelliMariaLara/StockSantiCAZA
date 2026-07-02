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
        var builder = new SqlConnectionStringBuilder(ConnectionStringResolver.Resolve(configuration));

        return Ok(new
        {
            status = "ok",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            os = RuntimeInformation.OSDescription,
            isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            sqlServer = builder.DataSource,
            database = builder.InitialCatalog,
            authMode = builder.IntegratedSecurity ? "integrated" : "sql",
            sqlUser = builder.IntegratedSecurity ? null : builder.UserID,
            tieneSqlPassword = ConnectionStringResolver.TieneSqlPassword(configuration),
            dataSourceConfigurado = configuration["Database:DataSource"],
            tieneProductionJson = System.IO.File.Exists(
                Path.Combine(AppContext.BaseDirectory, "appsettings.Production.json")),
            usaVariableEntorno = !string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"))
        });
    }

    [HttpGet("db")]
    public async Task<IActionResult> Database(CancellationToken ct)
    {
        var builder = new SqlConnectionStringBuilder(ConnectionStringResolver.Resolve(configuration))
        {
            ConnectTimeout = 10
        };

        try
        {
            await using var conexion = new SqlConnection(builder.ConnectionString);
            await conexion.OpenAsync(ct);
            return Ok(new { status = "ok", database = "connected", sqlServer = builder.DataSource });
        }
        catch (SqlException ex)
        {
            return StatusCode(503, new
            {
                status = "error",
                database = "error",
                sqlServer = builder.DataSource,
                sqlError = ex.Number,
                mensaje = ex.Message,
                ayuda = "Probá /api/health/sql-probe para ver qué método de conexión funciona en tu plan Ferozo."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new
            {
                status = "error",
                database = "error",
                sqlServer = builder.DataSource,
                mensaje = ex.Message,
                ayuda = "Probá /api/health/sql-probe"
            });
        }
    }

    [HttpGet("sql-probe")]
    public async Task<IActionResult> SqlProbe(CancellationToken ct)
    {
        var intentos = await FerozoSqlProbe.ProbarTodasAsync(configuration, ct);
        var exitoso = intentos.FirstOrDefault(i => i.ok);

        return Ok(new
        {
            status = exitoso is null ? "sin_conexion" : "ok",
            recomendacion = exitoso is null
                ? "Ningún método conectó. Abrí ticket en DonWeb (ver docs/FEROZO-CONEXION-TODOS-METODOS.md)."
                : $"Agregá en appsettings.Production.json: \"Database\": {{ \"DataSource\": \"{exitoso.dataSource}\" }}",
            metodoGanador = exitoso?.metodo,
            dataSourceGanador = exitoso?.dataSource,
            intentos = intentos.Select(i => new
            {
                i.metodo,
                i.dataSource,
                i.ok,
                i.sqlError,
                i.mensaje
            })
        });
    }
}
