-- =============================================================================
-- StockSantiCAZA - Actualización: módulo Proveedores
-- Base de datos: StockSantiCaza (SQL Server)
-- Cuándo ejecutar: si la base YA EXISTE y no tiene las tablas de proveedores.
-- Seguro de re-ejecutar: sí (verifica si las tablas existen antes de crearlas).
-- =============================================================================

USE [StockSantiCaza];
GO

IF OBJECT_ID(N'[dbo].[Proveedores]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Proveedores] (
        [Id]                int             NOT NULL IDENTITY(1,1),
        [NombreRazonSocial] nvarchar(160)   NOT NULL,
        [Telefono]          nvarchar(40)    NULL,
        [Email]             nvarchar(180)   NULL,
        [Domicilio]         nvarchar(220)   NULL,
        [Observaciones]     nvarchar(500)   NULL,
        [Activo]            bit             NOT NULL CONSTRAINT [DF_Proveedores_Activo] DEFAULT (1),
        CONSTRAINT [PK_Proveedores] PRIMARY KEY ([Id])
    );

    PRINT 'Tabla Proveedores creada.';
END
ELSE
BEGIN
    PRINT 'Tabla Proveedores ya existe.';
END
GO

IF OBJECT_ID(N'[dbo].[MovimientosProveedor]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MovimientosProveedor] (
        [Id]            int             NOT NULL IDENTITY(1,1),
        [ProveedorId]   int             NOT NULL,
        [Tipo]          int             NOT NULL,   -- 1 = Deuda, 2 = Pago
        [Fecha]         datetime2       NOT NULL,
        [Monto]         decimal(18,2)   NOT NULL,
        [Observaciones] nvarchar(500)   NULL,
        CONSTRAINT [PK_MovimientosProveedor] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MovimientosProveedor_Proveedores_ProveedorId]
            FOREIGN KEY ([ProveedorId])
            REFERENCES [dbo].[Proveedores] ([Id])
            ON DELETE CASCADE
    );

    CREATE INDEX [IX_MovimientosProveedor_ProveedorId]
        ON [dbo].[MovimientosProveedor] ([ProveedorId]);

    PRINT 'Tabla MovimientosProveedor creada.';
END
ELSE
BEGIN
    PRINT 'Tabla MovimientosProveedor ya existe.';
END
GO

-- Verificación rápida
SELECT
    t.name AS Tabla,
    p.rows AS Filas
FROM sys.tables t
INNER JOIN sys.partitions p ON t.object_id = p.object_id
WHERE t.name IN ('Proveedores', 'MovimientosProveedor')
  AND p.index_id IN (0, 1)
ORDER BY t.name;
GO
