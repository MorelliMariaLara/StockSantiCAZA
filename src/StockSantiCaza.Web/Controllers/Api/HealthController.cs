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
            ConnectTimeout = 8
        };

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));

        try
        {
            await using var conexion = new SqlConnection(builder.ConnectionString);
            await conexion.OpenAsync(timeout.Token);
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
                ayuda = "Probá de a uno: /api/health/sql-probe?metodo=1 hasta ?metodo=5"
            });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(503, new
            {
                status = "error",
                database = "timeout",
                sqlServer = builder.DataSource,
                mensaje = "La base no respondió en 10 segundos.",
                ayuda = "Probá /api/health/sql-probe?metodo=1"
            });
        }
    }

    /// <summary>
    /// Sin ?metodo= devuelve la lista (no conecta a SQL, no tumba el worker).
    /// Con ?metodo=1..5 prueba UN solo método por request.
    /// </summary>
    [HttpGet("sql-probe")]
    public async Task<IActionResult> SqlProbe([FromQuery] int? metodo, CancellationToken ct)
    {
        if (metodo is null)
        {
            return Ok(new
            {
                status = "info",
                mensaje = "Probá de a uno en el navegador (cada link hace una sola conexión):",
                metodos = FerozoSqlProbe.Metodos.Select(m => new
                {
                    m.id,
                    m.nombre,
                    url = $"/api/health/sql-probe?metodo={m.id}",
                    dataSourceSiFunciona = m.dataSource
                }),
                siConecta = "Agregá en appsettings.Production.json → Database.DataSource con el dataSource del método que funcione."
            });
        }

        if (metodo < 1 || metodo > 5)
        {
            return BadRequest(new { error = "metodo debe ser 1, 2, 3, 4 o 5." });
        }

        var resultado = await FerozoSqlProbe.ProbarMetodoAsync(configuration, metodo.Value, ct);
        return Ok(new
        {
            status = resultado.ok ? "ok" : "error",
            resultado.id,
            resultado.nombre,
            resultado.dataSource,
            resultado.ok,
            resultado.sqlError,
            resultado.mensaje,
            siguiente = resultado.ok
                ? $"Poné Database.DataSource = \"{resultado.dataSource}\" en appsettings.Production.json y republicá."
                : (resultado.id < 5 ? $"/api/health/sql-probe?metodo={resultado.id + 1}" : "Ninguno conectó: ticket a DonWeb (docs/FEROZO-CONEXION-TODOS-METODOS.md)")
        });
    }
}
