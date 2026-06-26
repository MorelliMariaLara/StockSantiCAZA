using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using StockSantiCaza.Web.Data;
using StockSantiCaza.Web.Models;

namespace StockSantiCaza.Web.Services.Usuarios;

public interface IUsuariosService
{
    Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Usuario>> ListarVendedoresActivosAsync(CancellationToken cancellationToken = default);

    Task GuardarAsync(UsuarioFormRequest request, CancellationToken cancellationToken = default);

    Task EliminarAsync(int usuarioId, int? usuarioActualId, CancellationToken cancellationToken = default);
}

public sealed class UsuarioFormRequest
{
    public int? Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Login { get; set; } = string.Empty;

    public string? Password { get; set; }

    public RolUsuario Rol { get; set; } = RolUsuario.Vendedor;
}

public class UsuariosService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    PasswordHasher<Usuario> passwordHasher) : IUsuariosService
{
    public async Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Usuarios
            .AsNoTracking()
            .OrderBy(x => x.Nombre)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Usuario>> ListarVendedoresActivosAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Usuarios
            .AsNoTracking()
            .Where(x => x.Activo)
            .OrderBy(x => x.Nombre)
            .ToListAsync(cancellationToken);
    }

    public async Task GuardarAsync(UsuarioFormRequest request, CancellationToken cancellationToken = default)
    {
        var nombre = request.Nombre.Trim();
        var login = request.Login.Trim().ToLowerInvariant();
        var password = request.Password?.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new InvalidOperationException("Debe indicar el nombre del usuario.");
        }

        if (string.IsNullOrWhiteSpace(login))
        {
            throw new InvalidOperationException("Debe indicar el usuario de acceso.");
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var loginDuplicado = await db.Usuarios.AnyAsync(x =>
            x.Login == login
            && (!request.Id.HasValue || x.Id != request.Id.Value),
            cancellationToken);

        if (loginDuplicado)
        {
            throw new InvalidOperationException($"Ya existe un usuario con login {login}.");
        }

        Usuario usuario;
        if (request.Id is null)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Debe indicar una contraseña para el usuario nuevo.");
            }

            usuario = new Usuario { Activo = true };
            db.Usuarios.Add(usuario);
            usuario.PasswordHash = passwordHasher.HashPassword(usuario, password);
        }
        else
        {
            usuario = await db.Usuarios.SingleAsync(x => x.Id == request.Id.Value, cancellationToken);
            if (!string.IsNullOrWhiteSpace(password))
            {
                usuario.PasswordHash = passwordHasher.HashPassword(usuario, password);
            }
        }

        usuario.Nombre = nombre;
        usuario.Login = login;
        usuario.Rol = request.Rol;
        usuario.Activo = true;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task EliminarAsync(
        int usuarioId,
        int? usuarioActualId,
        CancellationToken cancellationToken = default)
    {
        if (usuarioActualId == usuarioId)
        {
            throw new InvalidOperationException("No puede eliminar el usuario con el que está conectado.");
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var usuario = await db.Usuarios.SingleOrDefaultAsync(x => x.Id == usuarioId, cancellationToken)
            ?? throw new InvalidOperationException("El usuario seleccionado ya no existe.");

        if (usuario.Rol == RolUsuario.Administrador)
        {
            var quedanAdministradores = await db.Usuarios.AnyAsync(
                x => x.Id != usuarioId && x.Rol == RolUsuario.Administrador,
                cancellationToken);

            if (!quedanAdministradores)
            {
                throw new InvalidOperationException("No puede eliminar el último administrador del sistema.");
            }
        }

        db.Usuarios.Remove(usuario);
        await db.SaveChangesAsync(cancellationToken);
    }
}
