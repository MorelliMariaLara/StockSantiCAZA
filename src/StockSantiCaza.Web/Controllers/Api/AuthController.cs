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
    public async Task<ActionResult<UsuarioSesionDto>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var ok = await authService.IniciarSesionAsync(request.Login, request.Password, ct);
        if (!ok)
        {
            return Unauthorized(new { error = "Usuario o contraseña incorrectos." });
        }

        var usuario = authService.UsuarioActual;
        if (usuario is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "No se pudo iniciar la sesión." });
        }

        return Ok(UsuarioSesionDto.From(usuario));
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
