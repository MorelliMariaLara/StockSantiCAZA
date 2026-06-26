-- Reparar tabla Productos para que acepte clasificaciones de texto (Accesorios, Miras, etc.)
-- Ejecutar en SQL Server si no guarda productos.

-- 1) Crear columna temporal si Categoria sigue siendo int
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

-- 2) Copiar valores
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

-- 3) Quitar columna int
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

-- 4) Renombrar
IF COL_LENGTH('dbo.Productos', 'CategoriaNombre') IS NOT NULL
   AND COL_LENGTH('dbo.Productos', 'Categoria') IS NULL
BEGIN
    EXEC(N'EXEC sp_rename ''dbo.Productos.CategoriaNombre'', ''Categoria'', ''COLUMN'';');
END;
GO

-- 5) Permitir SKU y nombre vacíos (primero quitar índice)
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

-- 6) Recrear índice único de SKU solo cuando tiene valor
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

-- Verificación
SELECT c.name AS Columna, t.name AS Tipo
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
INNER JOIN sys.tables tb ON c.object_id = tb.object_id
WHERE tb.name = 'Productos' AND c.name IN ('Sku', 'Nombre', 'Categoria')
ORDER BY c.name;
GO
