-- StockSantiCAZA - Script inicial de tablas
-- Servidor: LARA-NB\SQLEXPRESS02
-- Base de datos: StockSantiCAZA
-- Ejecutar en SQL Server Management Studio (SSMS)

IF DB_ID(N'StockSantiCAZA') IS NULL
BEGIN
    CREATE DATABASE [StockSantiCAZA];
END
GO

USE [StockSantiCAZA];
GO

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

-- TABLA: Clientes
IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes (
        Id INT IDENTITY(1,1) NOT NULL,
        NombreRazonSocial NVARCHAR(160) NOT NULL,
        DniCuit NVARCHAR(20) NOT NULL,
        Email NVARCHAR(180) NULL,
        Telefono NVARCHAR(40) NULL,
        Domicilio NVARCHAR(220) NULL,
        CONSTRAINT PK_Clientes PRIMARY KEY (Id)
    );
END
GO

-- TABLA: Productos
IF OBJECT_ID(N'dbo.Productos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Productos (
        Id INT IDENTITY(1,1) NOT NULL,
        Sku NVARCHAR(40) NOT NULL,
        Nombre NVARCHAR(180) NOT NULL,
        Descripcion NVARCHAR(800) NULL,
        Categoria INT NOT NULL,
        Marca NVARCHAR(80) NULL,
        Modelo NVARCHAR(80) NULL,
        Calibre NVARCHAR(40) NULL,
        PrecioUnitario DECIMAL(18,2) NOT NULL,
        AlicuotaIva DECIMAL(5,2) NOT NULL,
        StockActual INT NOT NULL,
        StockMinimo INT NOT NULL,
        Activo BIT NOT NULL CONSTRAINT DF_Productos_Activo DEFAULT (1),
        CreadoEn DATETIME2 NOT NULL CONSTRAINT DF_Productos_CreadoEn DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Productos PRIMARY KEY (Id)
    );
END
GO

-- TABLA: CredencialesCLU
IF OBJECT_ID(N'dbo.CredencialesCLU', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CredencialesCLU (
        Id INT IDENTITY(1,1) NOT NULL,
        ClienteId INT NOT NULL,
        NumeroLegajo NVARCHAR(80) NOT NULL,
        FechaEmision DATE NOT NULL,
        FechaVencimiento DATE NOT NULL,
        CONSTRAINT PK_CredencialesCLU PRIMARY KEY (Id),
        CONSTRAINT FK_CredencialesCLU_Clientes_ClienteId
            FOREIGN KEY (ClienteId) REFERENCES dbo.Clientes (Id) ON DELETE CASCADE
    );
END
GO

-- TABLA: Ventas
IF OBJECT_ID(N'dbo.Ventas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ventas (
        Id INT IDENTITY(1,1) NOT NULL,
        Fecha DATETIME2 NOT NULL,
        ClienteId INT NOT NULL,
        TipoComprobante INT NOT NULL,
        Estado INT NOT NULL,
        PuntoVenta NVARCHAR(40) NULL,
        NumeroComprobante NVARCHAR(40) NULL,
        Cae NVARCHAR(30) NULL,
        CaeVencimiento DATE NULL,
        Subtotal DECIMAL(18,2) NOT NULL,
        DescuentoTotal DECIMAL(18,2) NOT NULL,
        IvaTotal DECIMAL(18,2) NOT NULL,
        Total DECIMAL(18,2) NOT NULL,
        Observaciones NVARCHAR(1000) NULL,
        CONSTRAINT PK_Ventas PRIMARY KEY (Id),
        CONSTRAINT FK_Ventas_Clientes_ClienteId
            FOREIGN KEY (ClienteId) REFERENCES dbo.Clientes (Id)
    );
END
GO

-- TABLA: Armas
IF OBJECT_ID(N'dbo.Armas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Armas (
        Id INT IDENTITY(1,1) NOT NULL,
        ProductoId INT NOT NULL,
        NumeroSerie NVARCHAR(80) NOT NULL,
        Marca NVARCHAR(80) NOT NULL,
        Modelo NVARCHAR(80) NOT NULL,
        Calibre NVARCHAR(40) NOT NULL,
        TipoArma INT NOT NULL,
        EstadoTramiteAnmac INT NOT NULL,
        NumeroTenenciaAnmac NVARCHAR(80) NULL,
        ClienteActualId INT NULL,
        FechaTransferencia DATETIME2 NULL,
        CONSTRAINT PK_Armas PRIMARY KEY (Id),
        CONSTRAINT FK_Armas_Clientes_ClienteActualId
            FOREIGN KEY (ClienteActualId) REFERENCES dbo.Clientes (Id) ON DELETE SET NULL,
        CONSTRAINT FK_Armas_Productos_ProductoId
            FOREIGN KEY (ProductoId) REFERENCES dbo.Productos (Id)
    );
END
GO

-- TABLA: MunicionLotes
IF OBJECT_ID(N'dbo.MunicionLotes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MunicionLotes (
        Id INT IDENTITY(1,1) NOT NULL,
        ProductoId INT NOT NULL,
        NumeroLote NVARCHAR(80) NOT NULL,
        Calibre NVARCHAR(40) NOT NULL,
        TipoMunicion INT NOT NULL,
        CantidadDisponible INT NOT NULL,
        FechaIngreso DATETIME2 NOT NULL,
        FechaVencimiento DATETIME2 NULL,
        CONSTRAINT PK_MunicionLotes PRIMARY KEY (Id),
        CONSTRAINT FK_MunicionLotes_Productos_ProductoId
            FOREIGN KEY (ProductoId) REFERENCES dbo.Productos (Id)
    );
END
GO

-- TABLA: DetallesVenta
IF OBJECT_ID(N'dbo.DetallesVenta', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DetallesVenta (
        Id INT IDENTITY(1,1) NOT NULL,
        VentaId INT NOT NULL,
        ProductoId INT NOT NULL,
        ArmaId INT NULL,
        MunicionLoteId INT NULL,
        Cantidad INT NOT NULL,
        PrecioUnitario DECIMAL(18,2) NOT NULL,
        Descuento DECIMAL(18,2) NOT NULL,
        AlicuotaIva DECIMAL(5,2) NOT NULL,
        Subtotal DECIMAL(18,2) NOT NULL,
        Iva DECIMAL(18,2) NOT NULL,
        Total DECIMAL(18,2) NOT NULL,
        CONSTRAINT PK_DetallesVenta PRIMARY KEY (Id),
        CONSTRAINT FK_DetallesVenta_Armas_ArmaId
            FOREIGN KEY (ArmaId) REFERENCES dbo.Armas (Id),
        CONSTRAINT FK_DetallesVenta_MunicionLotes_MunicionLoteId
            FOREIGN KEY (MunicionLoteId) REFERENCES dbo.MunicionLotes (Id),
        CONSTRAINT FK_DetallesVenta_Productos_ProductoId
            FOREIGN KEY (ProductoId) REFERENCES dbo.Productos (Id),
        CONSTRAINT FK_DetallesVenta_Ventas_VentaId
            FOREIGN KEY (VentaId) REFERENCES dbo.Ventas (Id) ON DELETE CASCADE
    );
END
GO

-- TABLA: MovimientosStock
IF OBJECT_ID(N'dbo.MovimientosStock', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MovimientosStock (
        Id INT IDENTITY(1,1) NOT NULL,
        Fecha DATETIME2 NOT NULL,
        ProductoId INT NOT NULL,
        VentaId INT NULL,
        ArmaId INT NULL,
        MunicionLoteId INT NULL,
        Tipo INT NOT NULL,
        Cantidad INT NOT NULL,
        StockResultante INT NOT NULL,
        Observacion NVARCHAR(500) NULL,
        CONSTRAINT PK_MovimientosStock PRIMARY KEY (Id),
        CONSTRAINT FK_MovimientosStock_Armas_ArmaId
            FOREIGN KEY (ArmaId) REFERENCES dbo.Armas (Id),
        CONSTRAINT FK_MovimientosStock_MunicionLotes_MunicionLoteId
            FOREIGN KEY (MunicionLoteId) REFERENCES dbo.MunicionLotes (Id),
        CONSTRAINT FK_MovimientosStock_Productos_ProductoId
            FOREIGN KEY (ProductoId) REFERENCES dbo.Productos (Id),
        CONSTRAINT FK_MovimientosStock_Ventas_VentaId
            FOREIGN KEY (VentaId) REFERENCES dbo.Ventas (Id)
    );
END
GO

-- INDICES
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Armas_ClienteActualId' AND object_id = OBJECT_ID(N'dbo.Armas'))
    CREATE INDEX IX_Armas_ClienteActualId ON dbo.Armas (ClienteActualId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Armas_NumeroSerie' AND object_id = OBJECT_ID(N'dbo.Armas'))
    CREATE UNIQUE INDEX IX_Armas_NumeroSerie ON dbo.Armas (NumeroSerie);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Armas_ProductoId' AND object_id = OBJECT_ID(N'dbo.Armas'))
    CREATE INDEX IX_Armas_ProductoId ON dbo.Armas (ProductoId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Clientes_DniCuit' AND object_id = OBJECT_ID(N'dbo.Clientes'))
    CREATE UNIQUE INDEX IX_Clientes_DniCuit ON dbo.Clientes (DniCuit);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CredencialesCLU_ClienteId' AND object_id = OBJECT_ID(N'dbo.CredencialesCLU'))
    CREATE UNIQUE INDEX IX_CredencialesCLU_ClienteId ON dbo.CredencialesCLU (ClienteId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CredencialesCLU_NumeroLegajo' AND object_id = OBJECT_ID(N'dbo.CredencialesCLU'))
    CREATE UNIQUE INDEX IX_CredencialesCLU_NumeroLegajo ON dbo.CredencialesCLU (NumeroLegajo);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DetallesVenta_ArmaId' AND object_id = OBJECT_ID(N'dbo.DetallesVenta'))
    CREATE INDEX IX_DetallesVenta_ArmaId ON dbo.DetallesVenta (ArmaId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DetallesVenta_MunicionLoteId' AND object_id = OBJECT_ID(N'dbo.DetallesVenta'))
    CREATE INDEX IX_DetallesVenta_MunicionLoteId ON dbo.DetallesVenta (MunicionLoteId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DetallesVenta_ProductoId' AND object_id = OBJECT_ID(N'dbo.DetallesVenta'))
    CREATE INDEX IX_DetallesVenta_ProductoId ON dbo.DetallesVenta (ProductoId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DetallesVenta_VentaId' AND object_id = OBJECT_ID(N'dbo.DetallesVenta'))
    CREATE INDEX IX_DetallesVenta_VentaId ON dbo.DetallesVenta (VentaId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MovimientosStock_ArmaId' AND object_id = OBJECT_ID(N'dbo.MovimientosStock'))
    CREATE INDEX IX_MovimientosStock_ArmaId ON dbo.MovimientosStock (ArmaId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MovimientosStock_MunicionLoteId' AND object_id = OBJECT_ID(N'dbo.MovimientosStock'))
    CREATE INDEX IX_MovimientosStock_MunicionLoteId ON dbo.MovimientosStock (MunicionLoteId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MovimientosStock_ProductoId' AND object_id = OBJECT_ID(N'dbo.MovimientosStock'))
    CREATE INDEX IX_MovimientosStock_ProductoId ON dbo.MovimientosStock (ProductoId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MovimientosStock_VentaId' AND object_id = OBJECT_ID(N'dbo.MovimientosStock'))
    CREATE INDEX IX_MovimientosStock_VentaId ON dbo.MovimientosStock (VentaId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MunicionLotes_ProductoId_NumeroLote' AND object_id = OBJECT_ID(N'dbo.MunicionLotes'))
    CREATE UNIQUE INDEX IX_MunicionLotes_ProductoId_NumeroLote ON dbo.MunicionLotes (ProductoId, NumeroLote);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Productos_Sku' AND object_id = OBJECT_ID(N'dbo.Productos'))
    CREATE UNIQUE INDEX IX_Productos_Sku ON dbo.Productos (Sku);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Ventas_ClienteId' AND object_id = OBJECT_ID(N'dbo.Ventas'))
    CREATE INDEX IX_Ventas_ClienteId ON dbo.Ventas (ClienteId);
GO

-- HISTORIAL EF CORE
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.__EFMigrationsHistory (
        MigrationId NVARCHAR(150) NOT NULL,
        ProductVersion NVARCHAR(32) NOT NULL,
        CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20260625131007_InitialCreate'
)
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20260625131007_InitialCreate', N'8.0.28');
END
GO

PRINT 'Script completado. Tablas creadas en StockSantiCAZA.';
GO
