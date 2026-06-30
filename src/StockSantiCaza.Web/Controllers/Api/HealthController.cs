using Microsoft.AspNetCore.Mvc;
using StockSantiCaza.Web.Data;

namespace StockSantiCaza.Web.Controllers.Api;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly DatabaseInitializationState _state;

    public HealthController(DatabaseInitializationState state)
    {
        _state = state;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var status = _state.Status switch
        {
            DatabaseInitStatus.Ready => "ready",
            DatabaseInitStatus.Skipped => "skipped",
            DatabaseInitStatus.Initializing => "initializing",
            DatabaseInitStatus.Failed => "failed",
            _ => "pending"
        };

        return Ok(new
        {
            database = status,
            ready = _state.IsReady,
            error = _state.ErrorMessage
        });
    }
}
