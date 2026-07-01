using Microsoft.AspNetCore.Mvc;
using StockSantiCaza.Web.Services.Auth;

namespace StockSantiCaza.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService authService;

    public AuthController(IAuthService authService)
    {
        this.authService = authService;
    }

    [HttpGet("me")]
    public ActionResult<UsuarioSesionDto> Me()
    {
        var usuario = authService.UsuarioActual;
        if (usuario is null)
        {
            return Unauthorized();
        }

        return Ok(UsuarioSesionDto.From(usuario));
    }

    [HttpPost("login")]
    public async Task<ActionResult<UsuarioSesionDto>> Login([FromBody] LoginRequest? request, CancellationToken ct)
    {
        if (request is null
            || string.IsNullOrWhiteSpace(request.Login)
            || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Ingrese usuario y contraseña." });
        }

        try
        {
            var usuario = await authService.IniciarSesionAsync(request.Login, request.Password, ct);
            if (usuario is null)
            {
                return Unauthorized(new { error = "Usuario o contraseña incorrectos." });
            }

            return Ok(UsuarioSesionDto.From(usuario));
        }
        catch (Microsoft.Data.SqlClient.SqlException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "No se pudo conectar con la base de datos. Revise appsettings.Production.json (Server=sql2016 y Database.SqlPassword)."
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Connection string", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("Cadena SQL", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await authService.CerrarSesionAsync();
        return Ok();
    }

    public sealed record LoginRequest(string Login, string Password);

    public sealed record UsuarioSesionDto(
        int Id,
        string Nombre,
        string Login,
        string Rol,
        bool EsAdministrador,
        bool PuedeEditarStock,
        bool PuedeVerMontosVentas,
        bool PuedeFiltrarVentasPorFecha)
    {
        public static UsuarioSesionDto From(UsuarioSesion sesion) => new(
            sesion.Id,
            sesion.Nombre,
            sesion.Login,
            sesion.Rol.ToString(),
            sesion.EsAdministrador,
            sesion.PuedeEditarStock,
            sesion.PuedeVerMontosVentas,
            sesion.PuedeFiltrarVentasPorFecha);
    }
}
