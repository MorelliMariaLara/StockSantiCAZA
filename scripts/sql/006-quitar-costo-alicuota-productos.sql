-- Quitar columnas obsoletas de Productos que impiden guardar
-- Ejecutar en SQL Server (Ferozo)

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

IF COL_LENGTH('dbo.Productos', 'CostoUnitario') IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[Productos] DROP COLUMN [CostoUnitario];');
END;
GO
