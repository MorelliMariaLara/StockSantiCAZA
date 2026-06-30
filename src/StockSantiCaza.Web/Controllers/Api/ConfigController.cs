using Microsoft.AspNetCore.Mvc;

namespace StockSantiCaza.Web.Controllers.Api;

[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            app = "StockSantiCAZA",
            apiBase = string.Empty,
            auth = "session-cookie",
            endpoints = new
            {
                health = "/api/health",
                login = "/api/auth/login",
                session = "/api/auth/me"
            }
        });
    }
}
