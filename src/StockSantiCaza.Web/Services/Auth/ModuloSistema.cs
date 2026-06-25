using StockSantiCaza.Web.Models;

namespace StockSantiCaza.Web.Services.Auth;

public enum ModuloSistema
{
    Dashboard,
    Clientes,
    Stock,
    Ventas,
    Reportes,
    Usuarios
}

public static class PermisosAcceso
{
    public static bool PuedeAcceder(RolUsuario rol, ModuloSistema modulo) =>
        rol switch
        {
            RolUsuario.Administrador => true,
            RolUsuario.Vendedor => modulo is ModuloSistema.Clientes or ModuloSistema.Stock or ModuloSistema.Ventas,
            _ => false
        };

    public static bool PuedeEditarStock(RolUsuario rol) =>
        rol == RolUsuario.Administrador;
}
