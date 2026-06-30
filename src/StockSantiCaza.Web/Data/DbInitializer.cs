using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Models;

namespace StockSantiCaza.Web.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (configuration.GetValue<bool>("Database:SkipInitialization"))
        {
            logger.LogInformation("[DbInitializer] Omitido (Database:SkipInitialization = true).");
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var tablaUsuariosExiste = await TablaExisteAsync(db, "Usuarios", cancellationToken);

        if (!tablaUsuariosExiste)
        {
            logger.LogInformation("[DbInitializer] La base no tiene tablas. Creando esquema inicial...");
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        logger.LogInformation("[DbInitializer] Aplicando actualizaciones de esquema...");
        await SchemaMigrationRunner.ApplyAsync(db, cancellationToken);

        if (!await db.Usuarios.AnyAsync(cancellationToken))
        {
            logger.LogInformation("[DbInitializer] Creando usuario administrador inicial...");
            await SeedAdminAsync(scope.ServiceProvider, db, cancellationToken);
        }
    }

    private static async Task<bool> TablaExisteAsync(
        ApplicationDbContext db,
        string nombreTabla,
        CancellationToken cancellationToken)
    {
        var conexion = db.Database.GetDbConnection();
        if (conexion.State != ConnectionState.Open)
        {
            await conexion.OpenAsync(cancellationToken);
        }

        await using var comando = conexion.CreateCommand();
        comando.CommandText = "SELECT OBJECT_ID(@nombre, 'U')";
        var parametro = comando.CreateParameter();
        parametro.ParameterName = "@nombre";
        parametro.Value = $"dbo.{nombreTabla}";
        comando.Parameters.Add(parametro);

        var resultado = await comando.ExecuteScalarAsync(cancellationToken);
        return resultado is not null and not DBNull;
    }

    private static async Task SeedAdminAsync(
        IServiceProvider services,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var passwordHasher = services.GetRequiredService<PasswordHasher<Usuario>>();
        var admin = new Usuario
        {
            Nombre = "Administrador",
            Login = "admin",
            Rol = RolUsuario.Administrador,
            Activo = true
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, "Admin123!");

        db.Usuarios.Add(admin);
        await db.SaveChangesAsync(cancellationToken);
    }
}
