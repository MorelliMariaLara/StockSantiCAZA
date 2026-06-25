using StockSantiCaza.Web.Models;

namespace StockSantiCaza.Web.Services.Auth;

public sealed record UsuarioSesion(
    int Id,
    string Nombre,
    string Login,
    RolUsuario Rol)
{
    public bool EsAdministrador => Rol == RolUsuario.Administrador;

    public bool PuedeAcceder(ModuloSistema modulo) => PermisosAcceso.PuedeAcceder(Rol, modulo);

    public bool PuedeEditarStock => PermisosAcceso.PuedeEditarStock(Rol);
}
