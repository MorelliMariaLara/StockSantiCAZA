using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Data;
using StockSantiCaza.Web.Models;

namespace StockSantiCaza.Web.Services.Auth;

public class AuthService : IAuthService
{
    private const string SessionKey = "StockSanti.UsuarioSesion";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;
    private readonly PasswordHasher<Usuario> passwordHasher;
    private readonly IHttpContextAccessor httpContextAccessor;

    public AuthService(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        PasswordHasher<Usuario> passwordHasher,
        IHttpContextAccessor httpContextAccessor)
    {
        this.dbContextFactory = dbContextFactory;
        this.passwordHasher = passwordHasher;
        this.httpContextAccessor = httpContextAccessor;
    }

    public UsuarioSesion? UsuarioActual
    {
        get
        {
            var session = httpContextAccessor.HttpContext?.Session;
            if (session is null)
            {
                return null;
            }

            var json = session.GetString(SessionKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var dto = JsonSerializer.Deserialize<UsuarioSesionDto>(json, JsonOptions);
            return dto?.ToSesion();
        }
        private set
        {
            var session = httpContextAccessor.HttpContext?.Session;
            if (session is null)
            {
                return;
            }

            if (value is null)
            {
                session.Remove(SessionKey);
                return;
            }

            session.SetString(SessionKey, JsonSerializer.Serialize(UsuarioSesionDto.FromSesion(value), JsonOptions));
        }
    }

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

    private sealed record UsuarioSesionDto(int Id, string Nombre, string Login, RolUsuario Rol)
    {
        public static UsuarioSesionDto FromSesion(UsuarioSesion sesion) =>
            new(sesion.Id, sesion.Nombre, sesion.Login, sesion.Rol);

        public UsuarioSesion ToSesion() => new(Id, Nombre, Login, Rol);
    }
}
