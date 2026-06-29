using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Data;
using StockSantiCaza.Web.Models;

namespace StockSantiCaza.Web.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;
    private readonly PasswordHasher<Usuario> passwordHasher;

    public AuthService(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        PasswordHasher<Usuario> passwordHasher)
    {
        this.dbContextFactory = dbContextFactory;
        this.passwordHasher = passwordHasher;
    }

    public UsuarioSesion? UsuarioActual { get; private set; }

    public event Action? SesionCambiada;

    public async Task<bool> IniciarSesionAsync(
        string login,
        string password,
        CancellationToken cancellationToken = default)
    {
        var loginNormalizado = login.Trim();
        if (string.IsNullOrWhiteSpace(loginNormalizado) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var usuario = await db.Usuarios
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Login == loginNormalizado && x.Activo, cancellationToken);

        if (usuario is null)
        {
            return false;
        }

        var resultado = passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password);
        if (resultado == PasswordVerificationResult.Failed)
        {
            return false;
        }

        UsuarioActual = new UsuarioSesion(usuario.Id, usuario.Nombre, usuario.Login, usuario.Rol);
        SesionCambiada?.Invoke();
        return true;
    }

    public Task CerrarSesionAsync()
    {
        UsuarioActual = null;
        SesionCambiada?.Invoke();
        return Task.CompletedTask;
    }

    public Task RestaurarSesionAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
