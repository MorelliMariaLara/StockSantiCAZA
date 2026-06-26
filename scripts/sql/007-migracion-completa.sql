-- =============================================================================
-- StockSantiCAZA - Migración completa de esquema (referencia manual)
-- Equivalente al script embebido que corre al iniciar la app.
-- Seguro de re-ejecutar: sí (idempotente).
-- =============================================================================

-- Migración idempotente de esquema StockSantiCAZA
-- Se ejecuta automáticamente al iniciar la app (cada batch por separado).

GO
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

GO
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

GO
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

GO
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

GO
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

GO
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Productos_Sku' AND object_id = OBJECT_ID(N'dbo.Productos')
)
BEGIN
    EXEC(N'DROP INDEX [IX_Productos_Sku] ON [dbo].[Productos];');
END;

GO
IF COL_LENGTH('dbo.Productos', 'Sku') IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Productos] ALTER COLUMN [Sku] nvarchar(40) NULL;');
END;

GO
IF COL_LENGTH('dbo.Productos', 'Nombre') IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Productos] ALTER COLUMN [Nombre] nvarchar(180) NULL;');
END;

GO
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

GO
IF COL_LENGTH('dbo.Productos', 'CostoUnitario') IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Productos] DROP COLUMN [CostoUnitario];');
END;

GO
IF COL_LENGTH('dbo.Productos', 'AlicuotaIva') IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Productos] DROP COLUMN [AlicuotaIva];');
END;

GO
IF COL_LENGTH('dbo.Productos', 'Alicuotalva') IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Productos] DROP COLUMN [Alicuotalva];');
END;

GO
IF OBJECT_ID(N'[dbo].[Clientes]', N'U') IS NOT NULL
AND EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Clientes_DniCuit' AND object_id = OBJECT_ID(N'dbo.Clientes') AND has_filter = 0
)
BEGIN
    EXEC(N'DROP INDEX [IX_Clientes_DniCuit] ON [dbo].[Clientes];');
END;

GO
IF OBJECT_ID(N'[dbo].[Clientes]', N'U') IS NOT NULL
AND COL_LENGTH('dbo.Clientes', 'NombreRazonSocial') IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Clientes] ALTER COLUMN [NombreRazonSocial] nvarchar(160) NULL;');
END;

GO
IF OBJECT_ID(N'[dbo].[Clientes]', N'U') IS NOT NULL
AND COL_LENGTH('dbo.Clientes', 'DniCuit') IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Clientes] ALTER COLUMN [DniCuit] nvarchar(20) NULL;');
END;

GO
IF OBJECT_ID(N'[dbo].[Clientes]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Clientes_DniCuit' AND object_id = OBJECT_ID(N'dbo.Clientes')
)
BEGIN
    EXEC(N'
        CREATE UNIQUE INDEX [IX_Clientes_DniCuit]
        ON [dbo].[Clientes] ([DniCuit])
        WHERE [DniCuit] IS NOT NULL AND [DniCuit] <> '''';
    ');
END;

GO
IF OBJECT_ID(N'[dbo].[Usuarios]', N'U') IS NOT NULL
AND EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Usuarios_Login' AND object_id = OBJECT_ID(N'dbo.Usuarios') AND has_filter = 0
)
BEGIN
    EXEC(N'DROP INDEX [IX_Usuarios_Login] ON [dbo].[Usuarios];');
END;

GO
IF OBJECT_ID(N'[dbo].[Usuarios]', N'U') IS NOT NULL
AND COL_LENGTH('dbo.Usuarios', 'Nombre') IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Usuarios] ALTER COLUMN [Nombre] nvarchar(120) NULL;');
END;

GO
IF OBJECT_ID(N'[dbo].[Usuarios]', N'U') IS NOT NULL
AND COL_LENGTH('dbo.Usuarios', 'Login') IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Usuarios] ALTER COLUMN [Login] nvarchar(60) NULL;');
END;

GO
IF OBJECT_ID(N'[dbo].[Usuarios]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Usuarios_Login' AND object_id = OBJECT_ID(N'dbo.Usuarios')
)
BEGIN
    EXEC(N'
        CREATE UNIQUE INDEX [IX_Usuarios_Login]
        ON [dbo].[Usuarios] ([Login])
        WHERE [Login] IS NOT NULL AND [Login] <> '''';
    ');
END;

GO
IF OBJECT_ID(N'[dbo].[CredencialesCLU]', N'U') IS NOT NULL
AND EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_CredencialesCLU_NumeroLegajo' AND object_id = OBJECT_ID(N'dbo.CredencialesCLU') AND has_filter = 0
)
BEGIN
    EXEC(N'DROP INDEX [IX_CredencialesCLU_NumeroLegajo] ON [dbo].[CredencialesCLU];');
END;

GO
IF OBJECT_ID(N'[dbo].[CredencialesCLU]', N'U') IS NOT NULL
AND COL_LENGTH('dbo.CredencialesCLU', 'NumeroLegajo') IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[CredencialesCLU] ALTER COLUMN [NumeroLegajo] nvarchar(80) NULL;');
END;

GO
IF OBJECT_ID(N'[dbo].[CredencialesCLU]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_CredencialesCLU_NumeroLegajo' AND object_id = OBJECT_ID(N'dbo.CredencialesCLU')
)
BEGIN
    EXEC(N'
        CREATE UNIQUE INDEX [IX_CredencialesCLU_NumeroLegajo]
        ON [dbo].[CredencialesCLU] ([NumeroLegajo])
        WHERE [NumeroLegajo] IS NOT NULL AND [NumeroLegajo] <> '''';
    ');
END;
