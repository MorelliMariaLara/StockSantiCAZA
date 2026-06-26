-- Quitar columnas obsoletas de Productos que impiden guardar
-- Ejecutar en SQL Server (Ferozo)

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
GO
