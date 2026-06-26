using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Models;

namespace StockSantiCaza.Web.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        await db.Database.EnsureCreatedAsync();
        await SchemaMigrationRunner.ApplyAsync(db);

        if (await db.Usuarios.AnyAsync())
        {
            return;
        }

        var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<Usuario>>();
        var admin = new Usuario
        {
            Nombre = "Administrador",
            Login = "admin",
            Rol = RolUsuario.Administrador,
            Activo = true
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, "Admin123!");

        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();
    }
}
