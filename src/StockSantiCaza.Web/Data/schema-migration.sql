-- Migración idempotente de esquema StockSantiCAZA
-- Se ejecuta automáticamente al iniciar la app (cada batch por separado).

--BATCH
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

--BATCH
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

--BATCH
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

--BATCH
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

--BATCH
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

--BATCH
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

--BATCH
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

--BATCH
IF COL_LENGTH('dbo.Productos', 'CategoriaNombre') IS NOT NULL
   AND COL_LENGTH('dbo.Productos', 'Categoria') IS NULL
BEGIN
    EXEC(N'EXEC sp_rename ''dbo.Productos.CategoriaNombre'', ''Categoria'', ''COLUMN'';');
END;

--BATCH
IF COL_LENGTH('dbo.Productos', 'Categoria') IS NULL
BEGIN
    ALTER TABLE [dbo].[Productos] ADD [Categoria] nvarchar(80) NULL;
END;

--BATCH
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

--BATCH
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Productos_Sku' AND object_id = OBJECT_ID(N'dbo.Productos')
)
BEGIN
    EXEC(N'DROP INDEX [IX_Productos_Sku] ON [dbo].[Productos];');
END;

--BATCH
IF COL_LENGTH('dbo.Productos', 'Sku') IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Productos] ALTER COLUMN [Sku] nvarchar(40) NULL;');
END;

--BATCH
IF COL_LENGTH('dbo.Productos', 'Nombre') IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Productos] ALTER COLUMN [Nombre] nvarchar(180) NULL;');
END;

--BATCH
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

--BATCH
IF COL_LENGTH('dbo.Productos', 'CostoUnitario') IS NOT NULL
   OR COL_LENGTH('dbo.Productos', 'AlicuotaIva') IS NOT NULL
   OR COL_LENGTH('dbo.Productos', 'Alicuotalva') IS NOT NULL
BEGIN
    EXEC(N'
        DECLARE @constraint sysname;
        DECLARE @sql nvarchar(max);

        WHILE 1 = 1
        BEGIN
            SELECT TOP 1 @constraint = dc.name
            FROM sys.default_constraints dc
            INNER JOIN sys.columns col
                ON dc.parent_object_id = col.object_id AND dc.parent_column_id = col.column_id
            WHERE dc.parent_object_id = OBJECT_ID(N''dbo.Productos'')
              AND col.name = N''CostoUnitario'';
            IF @constraint IS NULL BREAK;
            SET @sql = N''ALTER TABLE [dbo].[Productos] DROP CONSTRAINT '' + QUOTENAME(@constraint);
            EXEC sp_executesql @sql;
            SET @constraint = NULL;
        END;

        WHILE 1 = 1
        BEGIN
            SELECT TOP 1 @constraint = dc.name
            FROM sys.default_constraints dc
            INNER JOIN sys.columns col
                ON dc.parent_object_id = col.object_id AND dc.parent_column_id = col.column_id
            WHERE dc.parent_object_id = OBJECT_ID(N''dbo.Productos'')
              AND col.name = N''AlicuotaIva'';
            IF @constraint IS NULL BREAK;
            SET @sql = N''ALTER TABLE [dbo].[Productos] DROP CONSTRAINT '' + QUOTENAME(@constraint);
            EXEC sp_executesql @sql;
            SET @constraint = NULL;
        END;

        WHILE 1 = 1
        BEGIN
            SELECT TOP 1 @constraint = dc.name
            FROM sys.default_constraints dc
            INNER JOIN sys.columns col
                ON dc.parent_object_id = col.object_id AND dc.parent_column_id = col.column_id
            WHERE dc.parent_object_id = OBJECT_ID(N''dbo.Productos'')
              AND col.name = N''Alicuotalva'';
            IF @constraint IS NULL BREAK;
            SET @sql = N''ALTER TABLE [dbo].[Productos] DROP CONSTRAINT '' + QUOTENAME(@constraint);
            EXEC sp_executesql @sql;
            SET @constraint = NULL;
        END;

        IF COL_LENGTH(''dbo.Productos'', ''CostoUnitario'') IS NOT NULL
            ALTER TABLE [dbo].[Productos] DROP COLUMN [CostoUnitario];
        IF COL_LENGTH(''dbo.Productos'', ''AlicuotaIva'') IS NOT NULL
            ALTER TABLE [dbo].[Productos] DROP COLUMN [AlicuotaIva];
        IF COL_LENGTH(''dbo.Productos'', ''Alicuotalva'') IS NOT NULL
            ALTER TABLE [dbo].[Productos] DROP COLUMN [Alicuotalva];
    ');
END;

--BATCH
IF OBJECT_ID(N'[dbo].[Clientes]', N'U') IS NOT NULL
AND EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Clientes_DniCuit' AND object_id = OBJECT_ID(N'dbo.Clientes')
)
BEGIN
    EXEC(N'DROP INDEX [IX_Clientes_DniCuit] ON [dbo].[Clientes];');
END;

--BATCH
IF OBJECT_ID(N'[dbo].[Clientes]', N'U') IS NOT NULL
AND COL_LENGTH('dbo.Clientes', 'NombreRazonSocial') IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Clientes] ALTER COLUMN [NombreRazonSocial] nvarchar(160) NULL;');
END;

--BATCH
IF OBJECT_ID(N'[dbo].[Clientes]', N'U') IS NOT NULL
AND COL_LENGTH('dbo.Clientes', 'DniCuit') IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Clientes] ALTER COLUMN [DniCuit] nvarchar(20) NULL;');
END;

--BATCH
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

--BATCH
IF OBJECT_ID(N'[dbo].[Usuarios]', N'U') IS NOT NULL
AND EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Usuarios_Login' AND object_id = OBJECT_ID(N'dbo.Usuarios')
)
BEGIN
    EXEC(N'DROP INDEX [IX_Usuarios_Login] ON [dbo].[Usuarios];');
END;

--BATCH
IF OBJECT_ID(N'[dbo].[Usuarios]', N'U') IS NOT NULL
AND COL_LENGTH('dbo.Usuarios', 'Nombre') IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Usuarios] ALTER COLUMN [Nombre] nvarchar(120) NULL;');
END;

--BATCH
IF OBJECT_ID(N'[dbo].[Usuarios]', N'U') IS NOT NULL
AND COL_LENGTH('dbo.Usuarios', 'Login') IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Usuarios] ALTER COLUMN [Login] nvarchar(60) NULL;');
END;

--BATCH
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

--BATCH
IF OBJECT_ID(N'[dbo].[CredencialesCLU]', N'U') IS NOT NULL
AND EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_CredencialesCLU_NumeroLegajo' AND object_id = OBJECT_ID(N'dbo.CredencialesCLU')
)
BEGIN
    EXEC(N'DROP INDEX [IX_CredencialesCLU_NumeroLegajo] ON [dbo].[CredencialesCLU];');
END;

--BATCH
IF OBJECT_ID(N'[dbo].[CredencialesCLU]', N'U') IS NOT NULL
AND COL_LENGTH('dbo.CredencialesCLU', 'NumeroLegajo') IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[CredencialesCLU] ALTER COLUMN [NumeroLegajo] nvarchar(80) NULL;');
END;

--BATCH
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
