using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Data;

namespace StockSantiCaza.Web.Controllers.Api;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;

    public HealthController(IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    [HttpGet]
    public IActionResult Get() =>
        Ok(new { status = "ok", environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production" });

    [HttpGet("db")]
    public async Task<IActionResult> Database(CancellationToken ct)
    {
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);
            var ok = await db.Database.CanConnectAsync(ct);
            return ok
                ? Ok(new { status = "ok", database = "connected" })
                : StatusCode(503, new { status = "error", database = "unreachable" });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { status = "error", database = ex.Message });
        }
    }
}
