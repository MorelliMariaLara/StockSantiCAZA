-- Categorías de stock + migración Productos.Categoria (int -> nvarchar)
-- Ejecutar en SQL Server (Ferozo). Cada bloque GO por separado si hace falta.

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

IF COL_LENGTH('dbo.Productos', 'Categoria') = 4
   AND COL_LENGTH('dbo.Productos', 'CategoriaNombre') IS NULL
BEGIN
    ALTER TABLE [dbo].[Productos] ADD [CategoriaNombre] nvarchar(80) NULL;
END;
GO

IF COL_LENGTH('dbo.Productos', 'CategoriaNombre') IS NOT NULL
   AND COL_LENGTH('dbo.Productos', 'Categoria') = 4
BEGIN
    EXEC(N'
        UPDATE [dbo].[Productos]
        SET [CategoriaNombre] = CASE [Categoria]
            WHEN 2 THEN N''Arma''
            WHEN 3 THEN N''Munición''
            ELSE N''General''
        END;
    ');
END;
GO

IF COL_LENGTH('dbo.Productos', 'Categoria') = 4
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Productos] DROP COLUMN [Categoria];');
END;
GO

IF COL_LENGTH('dbo.Productos', 'CategoriaNombre') IS NOT NULL
   AND COL_LENGTH('dbo.Productos', 'Categoria') IS NULL
BEGIN
    EXEC(N'EXEC sp_rename ''dbo.Productos.CategoriaNombre'', ''Categoria'', ''COLUMN'';');
END;
GO

IF COL_LENGTH('dbo.Productos', 'Categoria') IS NULL
BEGIN
    ALTER TABLE [dbo].[Productos] ADD [Categoria] nvarchar(80) NULL;
END;
GO

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
