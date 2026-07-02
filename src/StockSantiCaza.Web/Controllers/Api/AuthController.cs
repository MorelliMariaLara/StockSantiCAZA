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
        try
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
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "No se pudo conectar a la base de datos.",
                sqlError = ex.Number,
                detalle = ex.Message,
                ayuda = "Abrí /api/health/sql-probe en el navegador para diagnosticar la conexión en Ferozo."
            });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "La base de datos no respondió a tiempo.",
                ayuda = "Abrí /api/health/sql-probe para ver qué servidor SQL funciona en tu hosting."
            });
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
