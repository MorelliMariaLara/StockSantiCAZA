-- Categorías de stock + migración Productos.Categoria (int -> nvarchar)
-- Base: w400048_santicazarmeria (SQL Server / Ferozo)
-- Ejecutar en el administrador SQL de Ferozo o SSMS, paso a paso (cada bloque GO por separado).

-- 1) Tabla de clasificaciones
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
GO

-- 2) Agregar columna temporal (solo si Categoria sigue siendo int)
IF COL_LENGTH('dbo.Productos', 'Categoria') = 4
   AND COL_LENGTH('dbo.Productos', 'CategoriaNombre') IS NULL
BEGIN
    ALTER TABLE [dbo].[Productos] ADD [CategoriaNombre] nvarchar(80) NULL;
END;
GO

-- 3) Copiar valores int -> texto
IF COL_LENGTH('dbo.Productos', 'CategoriaNombre') IS NOT NULL
   AND COL_LENGTH('dbo.Productos', 'Categoria') = 4
BEGIN
    UPDATE [dbo].[Productos]
    SET [CategoriaNombre] = CASE [Categoria]
        WHEN 2 THEN N'Arma'
        WHEN 3 THEN N'Munición'
        ELSE N'General'
    END;
END;
GO

-- 4) Quitar columna int vieja
IF COL_LENGTH('dbo.Productos', 'Categoria') = 4
BEGIN
    ALTER TABLE [dbo].[Productos] DROP COLUMN [Categoria];
END;
GO

-- 5) Renombrar CategoriaNombre -> Categoria
IF COL_LENGTH('dbo.Productos', 'CategoriaNombre') IS NOT NULL
   AND COL_LENGTH('dbo.Productos', 'Categoria') IS NULL
BEGIN
    EXEC sp_rename 'dbo.Productos.CategoriaNombre', 'Categoria', 'COLUMN';
END;
GO

-- 6) Si no existe ninguna columna Categoria, crearla
IF COL_LENGTH('dbo.Productos', 'Categoria') IS NULL
BEGIN
    ALTER TABLE [dbo].[Productos] ADD [Categoria] nvarchar(80) NULL;
END;
GO

-- 7) Datos iniciales de clasificaciones
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
GO
