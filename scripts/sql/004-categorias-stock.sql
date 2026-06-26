-- Categorías de stock personalizables y migración de Productos.Categoria (int -> nvarchar)
-- Ejecutar en SQL Server si la app no aplicó el cambio automáticamente al iniciar.

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

IF COL_LENGTH('dbo.Productos', 'Categoria') = 4
BEGIN
    ALTER TABLE [dbo].[Productos] ADD [CategoriaNombre] nvarchar(80) NULL;
    UPDATE [dbo].[Productos]
    SET [CategoriaNombre] = CASE [Categoria]
        WHEN 2 THEN N'Arma'
        WHEN 3 THEN N'Munición'
        ELSE N'General'
    END;
    ALTER TABLE [dbo].[Productos] DROP COLUMN [Categoria];
    EXEC sp_rename 'dbo.Productos.CategoriaNombre', 'Categoria', 'COLUMN';
END;

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
