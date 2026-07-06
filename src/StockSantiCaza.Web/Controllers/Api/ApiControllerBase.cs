using Microsoft.AspNetCore.Mvc;
using StockSantiCaza.Web.Helpers;
using StockSantiCaza.Web.Services.Auth;

namespace StockSantiCaza.Web.Controllers.Api;

public abstract class ApiControllerBase : ControllerBase
{
    protected readonly IAuthService AuthService;

    protected ApiControllerBase(IAuthService authService)
    {
        AuthService = authService;
    }

    protected UsuarioSesion RequireAuth()
    {
        var usuario = AuthService.UsuarioActual;
        if (usuario is null)
        {
            throw new UnauthorizedAccessException("Debe iniciar sesión.");
        }

        return usuario;
    }

    protected UsuarioSesion RequireAdmin()
    {
        var usuario = RequireAuth();
        if (!usuario.EsAdministrador)
        {
            throw new UnauthorizedAccessException("Solo administradores.");
        }

        return usuario;
    }

    protected UsuarioSesion RequireModulo(ModuloSistema modulo)
    {
        var usuario = RequireAuth();
        if (!usuario.PuedeAcceder(modulo))
        {
            throw new UnauthorizedAccessException("No tiene permisos para este módulo.");
        }

        return usuario;
    }

    protected UsuarioSesion RequireAlgunoDeModulos(params ModuloSistema[] modulos)
    {
        var usuario = RequireAuth();
        if (modulos.Length == 0 || !modulos.Any(usuario.PuedeAcceder))
        {
            throw new UnauthorizedAccessException("No tiene permisos para este módulo.");
        }

        return usuario;
    }

    protected ActionResult HandleError(Exception ex) =>
        ex switch
        {
            UnauthorizedAccessException => Unauthorized(new { error = ex.Message }),
            InvalidOperationException => BadRequest(new { error = ExceptionHelper.ObtenerMensaje(ex) }),
            _ => StatusCode(500, new { error = ExceptionHelper.ObtenerMensaje(ex) })
        };
}
