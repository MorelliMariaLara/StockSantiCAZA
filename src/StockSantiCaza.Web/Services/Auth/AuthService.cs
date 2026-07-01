using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Data;
using StockSantiCaza.Web.Models;

namespace StockSantiCaza.Web.Services.Auth;

public class AuthService : IAuthService
{
    private const string CookieName = "StockSanti.Auth";
    private const string ProtectorPurpose = "StockSanti.Auth.v1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;
    private readonly PasswordHasher<Usuario> passwordHasher;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IDataProtectionProvider dataProtectionProvider;

    public AuthService(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        PasswordHasher<Usuario> passwordHasher,
        IHttpContextAccessor httpContextAccessor,
        IDataProtectionProvider dataProtectionProvider)
    {
        this.dbContextFactory = dbContextFactory;
        this.passwordHasher = passwordHasher;
        this.httpContextAccessor = httpContextAccessor;
        this.dataProtectionProvider = dataProtectionProvider;
    }

    public UsuarioSesion? UsuarioActual
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            if (context is null || !context.Request.Cookies.TryGetValue(CookieName, out var valor))
            {
                return null;
            }

            try
            {
                var json = dataProtectionProvider
                    .CreateProtector(ProtectorPurpose)
                    .Unprotect(valor);
                var dto = JsonSerializer.Deserialize<UsuarioSesionDto>(json, JsonOptions);
                return dto?.ToSesion();
            }
            catch
            {
                return null;
            }
        }
        private set
        {
            var context = httpContextAccessor.HttpContext;
            if (context is null)
            {
                return;
            }

            if (value is null)
            {
                context.Response.Cookies.Delete(CookieName);
                return;
            }

            var json = JsonSerializer.Serialize(UsuarioSesionDto.FromSesion(value), JsonOptions);
            var protegido = dataProtectionProvider
                .CreateProtector(ProtectorPurpose)
                .Protect(json);

            context.Response.Cookies.Append(CookieName, protegido, new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                Path = "/",
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });
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

        UsuarioActual = new UsuarioSesion(usuario.Id, usuario.Nombre ?? string.Empty, usuario.Login ?? string.Empty, usuario.Rol);
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
