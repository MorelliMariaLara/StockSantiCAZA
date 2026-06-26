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
        await EnsureCategoriasStockSchemaAsync(db);
        await EnsureProductosSchemaAsync(db);

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

    private static async Task EnsureCategoriasStockSchemaAsync(ApplicationDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[dbo].[CategoriasStock]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[CategoriasStock] (
                    [Id] int NOT NULL IDENTITY,
                    [Nombre] nvarchar(80) NOT NULL,
                    [RequiereSerie] bit NOT NULL CONSTRAINT [DF_CategoriasStock_RequiereSerie] DEFAULT CAST(0 AS bit),
                    [RequiereLote] bit NOT NULL CONSTRAINT [DF_CategoriasStock_RequiereLote] DEFAULT CAST(0 AS bit),
                    [Activo] bit NOT NULL CONSTRAINT [DF_CategoriasStock_Activo] DEFAULT CAST(1 AS bit),
                    CONSTRAINT [PK_CategoriasStock] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_CategoriasStock_Nombre] ON [dbo].[CategoriasStock] ([Nombre]);
            END;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (SELECT 1 FROM [dbo].[CategoriasStock])
            BEGIN
                INSERT INTO [dbo].[CategoriasStock] ([Nombre], [RequiereSerie], [RequiereLote], [Activo])
                VALUES
                    (N'General', 0, 0, 1),
                    (N'Arma', 1, 0, 1),
                    (N'Munición', 0, 1, 1),
                    (N'Miras', 0, 0, 1),
                    (N'Accesorios', 0, 0, 1);
            END;
            """);
    }

    private static async Task EnsureProductosSchemaAsync(ApplicationDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF EXISTS (
                SELECT 1
                FROM sys.columns c
                INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
                INNER JOIN sys.tables tb ON c.object_id = tb.object_id
                WHERE tb.name = 'Productos' AND c.name = 'Categoria' AND t.name = 'int'
            )
            AND COL_LENGTH('dbo.Productos', 'CategoriaNombre') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Productos] ADD [CategoriaNombre] nvarchar(80) NULL;
            END;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.Productos', 'CategoriaNombre') IS NOT NULL
            AND EXISTS (
                SELECT 1
                FROM sys.columns c
                INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
                INNER JOIN sys.tables tb ON c.object_id = tb.object_id
                WHERE tb.name = 'Productos' AND c.name = 'Categoria' AND t.name = 'int'
            )
            BEGIN
                EXEC(N'
                    UPDATE [dbo].[Productos]
                    SET [CategoriaNombre] = CASE [Categoria]
                        WHEN 2 THEN N''Arma''
                        WHEN 3 THEN N''Munición''
                        ELSE N''General''
                    END
                    WHERE [CategoriaNombre] IS NULL;
                ');
            END;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF EXISTS (
                SELECT 1
                FROM sys.columns c
                INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
                INNER JOIN sys.tables tb ON c.object_id = tb.object_id
                WHERE tb.name = 'Productos' AND c.name = 'Categoria' AND t.name = 'int'
            )
            BEGIN
                EXEC(N'ALTER TABLE [dbo].[Productos] DROP COLUMN [Categoria];');
            END;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.Productos', 'CategoriaNombre') IS NOT NULL
               AND COL_LENGTH('dbo.Productos', 'Categoria') IS NULL
            BEGIN
                EXEC(N'EXEC sp_rename ''dbo.Productos.CategoriaNombre'', ''Categoria'', ''COLUMN'';');
            END;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.Productos', 'Categoria') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Productos] ADD [Categoria] nvarchar(80) NULL;
            END;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.Productos', 'Categoria') IS NOT NULL
            AND EXISTS (
                SELECT 1
                FROM sys.columns c
                INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
                INNER JOIN sys.tables tb ON c.object_id = tb.object_id
                WHERE tb.name = 'Productos' AND c.name = 'Categoria' AND t.name <> 'nvarchar'
            )
            BEGIN
                EXEC(N'
                    ALTER TABLE [dbo].[Productos] ADD [CategoriaTxt] nvarchar(80) NULL;
                    UPDATE [dbo].[Productos]
                    SET [CategoriaTxt] = CASE [Categoria]
                        WHEN 2 THEN N''Arma''
                        WHEN 3 THEN N''Munición''
                        ELSE N''General''
                    END;
                    ALTER TABLE [dbo].[Productos] DROP COLUMN [Categoria];
                    EXEC sp_rename ''dbo.Productos.CategoriaTxt'', ''Categoria'', ''COLUMN'';
                ');
            END;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'IX_Productos_Sku' AND object_id = OBJECT_ID(N'dbo.Productos')
            )
            BEGIN
                EXEC(N'DROP INDEX [IX_Productos_Sku] ON [dbo].[Productos];');
            END;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.Productos', 'Sku') IS NOT NULL
            BEGIN
                EXEC(N'ALTER TABLE [dbo].[Productos] ALTER COLUMN [Sku] nvarchar(40) NULL;');
            END;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.Productos', 'Nombre') IS NOT NULL
            BEGIN
                EXEC(N'ALTER TABLE [dbo].[Productos] ALTER COLUMN [Nombre] nvarchar(180) NULL;');
            END;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'IX_Productos_Sku' AND object_id = OBJECT_ID(N'dbo.Productos')
            )
            BEGIN
                EXEC(N'
                    CREATE UNIQUE INDEX [IX_Productos_Sku]
                    ON [dbo].[Productos] ([Sku])
                    WHERE [Sku] IS NOT NULL AND [Sku] <> '''';
                ');
            END;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.Productos', 'CostoUnitario') IS NOT NULL
            BEGIN
                EXEC(N'ALTER TABLE [dbo].[Productos] DROP COLUMN [CostoUnitario];');
            END;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.Productos', 'AlicuotaIva') IS NOT NULL
            BEGIN
                EXEC(N'ALTER TABLE [dbo].[Productos] DROP COLUMN [AlicuotaIva];');
            END;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.Productos', 'Alicuotalva') IS NOT NULL
            BEGIN
                EXEC(N'ALTER TABLE [dbo].[Productos] DROP COLUMN [Alicuotalva];');
            END;
            """);
    }
}
