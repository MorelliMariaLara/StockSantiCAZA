namespace StockSantiCaza.Web.Services.Auth;

public interface IAuthService
{
    UsuarioSesion? UsuarioActual { get; }

    event Action? SesionCambiada;

    Task<UsuarioSesion?> IniciarSesionAsync(string login, string password, CancellationToken cancellationToken = default);

    Task CerrarSesionAsync();

    Task RestaurarSesionAsync(CancellationToken cancellationToken = default);
}
