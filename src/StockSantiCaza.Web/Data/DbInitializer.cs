using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Data;
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
        await EnsureProveedoresSchemaAsync(db);

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

    private static async Task EnsureProveedoresSchemaAsync(ApplicationDbContext db)
    {
        const string sql = """
            IF OBJECT_ID(N'[dbo].[Proveedores]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Proveedores] (
                    [Id] int NOT NULL IDENTITY,
                    [NombreRazonSocial] nvarchar(160) NOT NULL,
                    [Telefono] nvarchar(40) NULL,
                    [Email] nvarchar(180) NULL,
                    [Domicilio] nvarchar(220) NULL,
                    [Observaciones] nvarchar(500) NULL,
                    [Activo] bit NOT NULL CONSTRAINT [DF_Proveedores_Activo] DEFAULT CAST(1 AS bit),
                    CONSTRAINT [PK_Proveedores] PRIMARY KEY ([Id])
                );
            END;

            IF OBJECT_ID(N'[dbo].[MovimientosProveedor]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[MovimientosProveedor] (
                    [Id] int NOT NULL IDENTITY,
                    [ProveedorId] int NOT NULL,
                    [Tipo] int NOT NULL,
                    [Fecha] datetime2 NOT NULL,
                    [Monto] decimal(18,2) NOT NULL,
                    [Observaciones] nvarchar(500) NULL,
                    CONSTRAINT [PK_MovimientosProveedor] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_MovimientosProveedor_Proveedores_ProveedorId]
                        FOREIGN KEY ([ProveedorId]) REFERENCES [dbo].[Proveedores] ([Id]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_MovimientosProveedor_ProveedorId] ON [dbo].[MovimientosProveedor] ([ProveedorId]);
            END;
            """;

        await db.Database.ExecuteSqlRawAsync(sql);
    }
}
