-- =============================================================================
-- StockSantiCAZA - Esquema completo para MySQL 8.x
-- Migración desde SQL Server (EF Core EnsureCreated)
-- Charset: utf8mb4 (soporta acentos y emojis)
-- =============================================================================

CREATE DATABASE IF NOT EXISTS `StockSantiCaza`
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE `StockSantiCaza`;

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- -----------------------------------------------------------------------------
-- Limpieza (opcional: descomentar si querés recrear desde cero)
-- -----------------------------------------------------------------------------
/*
DROP TABLE IF EXISTS `MovimientosProveedor`;
DROP TABLE IF EXISTS `MovimientosStock`;
DROP TABLE IF EXISTS `DetallesVenta`;
DROP TABLE IF EXISTS `Ventas`;
DROP TABLE IF EXISTS `CredencialesCLU`;
DROP TABLE IF EXISTS `Armas`;
DROP TABLE IF EXISTS `MunicionLotes`;
DROP TABLE IF EXISTS `Proveedores`;
DROP TABLE IF EXISTS `Clientes`;
DROP TABLE IF EXISTS `Productos`;
DROP TABLE IF EXISTS `Usuarios`;
*/

-- -----------------------------------------------------------------------------
-- Usuarios
-- RolUsuario: 1=Administrador, 2=Vendedor
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Usuarios` (
    `Id`           INT          NOT NULL AUTO_INCREMENT,
    `Nombre`       VARCHAR(120) NOT NULL,
    `Login`        VARCHAR(60)  NOT NULL,
    `PasswordHash` VARCHAR(256) NOT NULL,
    `Rol`          INT          NOT NULL,
    `Activo`       TINYINT(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Usuarios_Login` (`Login`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------------------------------
-- Productos
-- ProductoCategoria: 1=General, 2=Arma, 3=Municion
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Productos` (
    `Id`             INT            NOT NULL AUTO_INCREMENT,
    `Sku`            VARCHAR(40)    NOT NULL,
    `Nombre`         VARCHAR(180)   NOT NULL,
    `Descripcion`    VARCHAR(800)   NULL,
    `Categoria`      INT            NOT NULL DEFAULT 1,
    `Marca`          VARCHAR(80)    NULL,
    `Modelo`         VARCHAR(80)    NULL,
    `Calibre`        VARCHAR(40)    NULL,
    `PrecioUnitario` DECIMAL(18,2)  NOT NULL DEFAULT 0.00,
    `CostoUnitario`  DECIMAL(18,2)  NOT NULL DEFAULT 0.00,
    `StockActual`    INT            NOT NULL DEFAULT 0,
    `StockMinimo`    INT            NOT NULL DEFAULT 1,
    `Activo`         TINYINT(1)     NOT NULL DEFAULT 1,
    `CreadoEn`       DATETIME(6)    NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Productos_Sku` (`Sku`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------------------------------
-- Clientes
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Clientes` (
    `Id`                INT          NOT NULL AUTO_INCREMENT,
    `NombreRazonSocial` VARCHAR(160) NOT NULL,
    `DniCuit`           VARCHAR(20)  NOT NULL,
    `Email`             VARCHAR(180) NULL,
    `Telefono`          VARCHAR(40)  NULL,
    `Domicilio`         VARCHAR(220) NULL,
    `Activo`            TINYINT(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Clientes_DniCuit` (`DniCuit`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------------------------------
-- Proveedores
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Proveedores` (
    `Id`                INT          NOT NULL AUTO_INCREMENT,
    `NombreRazonSocial` VARCHAR(160) NOT NULL,
    `Telefono`          VARCHAR(40)  NULL,
    `Email`             VARCHAR(180) NULL,
    `Domicilio`         VARCHAR(220) NULL,
    `Observaciones`     VARCHAR(500) NULL,
    `Activo`            TINYINT(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------------------------------
-- Credenciales CLU (1:1 con Cliente)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `CredencialesCLU` (
    `Id`               INT         NOT NULL AUTO_INCREMENT,
    `ClienteId`        INT         NOT NULL,
    `NumeroLegajo`     VARCHAR(80) NOT NULL,
    `FechaEmision`     DATE        NOT NULL,
    `FechaVencimiento` DATE        NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_CredencialesCLU_NumeroLegajo` (`NumeroLegajo`),
    UNIQUE KEY `IX_CredencialesCLU_ClienteId` (`ClienteId`),
    CONSTRAINT `FK_CredencialesCLU_Clientes_ClienteId`
        FOREIGN KEY (`ClienteId`) REFERENCES `Clientes` (`Id`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------------------------------
-- Armas
-- TipoArma: 1=Pistola, 2=Revolver, 3=Escopeta, 4=Rifle, 5=Carabina,
--           6=AireComprimido, 99=Otro
-- EstadoTramiteAnmac: 0=NoAplica, 1=PendienteAutorizacion, 2=Autorizado,
--                     3=Entregado, 4=Observado, 5=Rechazado
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Armas` (
    `Id`                  INT         NOT NULL AUTO_INCREMENT,
    `ProductoId`          INT         NOT NULL,
    `NumeroSerie`         VARCHAR(80) NOT NULL,
    `Marca`               VARCHAR(80) NOT NULL,
    `Modelo`              VARCHAR(80) NOT NULL,
    `Calibre`             VARCHAR(40) NOT NULL,
    `TipoArma`            INT         NOT NULL,
    `EstadoTramiteAnmac`  INT         NOT NULL DEFAULT 1,
    `NumeroTenenciaAnmac` VARCHAR(80) NULL,
    `ClienteActualId`     INT         NULL,
    `FechaTransferencia`  DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Armas_NumeroSerie` (`NumeroSerie`),
    KEY `IX_Armas_ProductoId` (`ProductoId`),
    KEY `IX_Armas_ClienteActualId` (`ClienteActualId`),
    CONSTRAINT `FK_Armas_Productos_ProductoId`
        FOREIGN KEY (`ProductoId`) REFERENCES `Productos` (`Id`)
        ON DELETE RESTRICT,
    CONSTRAINT `FK_Armas_Clientes_ClienteActualId`
        FOREIGN KEY (`ClienteActualId`) REFERENCES `Clientes` (`Id`)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------------------------------
-- Lotes de munición
-- TipoMunicion: 1=Cartucho, 2=Bala, 3=Posta, 4=Fulminante, 99=Otro
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `MunicionLotes` (
    `Id`                 INT         NOT NULL AUTO_INCREMENT,
    `ProductoId`         INT         NOT NULL,
    `NumeroLote`         VARCHAR(80) NOT NULL,
    `Calibre`            VARCHAR(40) NOT NULL,
    `TipoMunicion`       INT         NOT NULL,
    `CantidadDisponible` INT         NOT NULL DEFAULT 0,
    `FechaIngreso`       DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `FechaVencimiento`   DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_MunicionLotes_ProductoId_NumeroLote` (`ProductoId`, `NumeroLote`),
    CONSTRAINT `FK_MunicionLotes_Productos_ProductoId`
        FOREIGN KEY (`ProductoId`) REFERENCES `Productos` (`Id`)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------------------------------
-- Ventas
-- TipoComprobante: 0=Presupuesto, 1=FacturaA, 2=FacturaB, 3=FacturaC
-- EstadoVenta: 0=Borrador, 1=Confirmada, 2=Facturada, 3=Anulada
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Ventas` (
    `Id`                INT            NOT NULL AUTO_INCREMENT,
    `Fecha`             DATETIME(6)    NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `ClienteId`         INT            NOT NULL,
    `TipoComprobante`   INT            NOT NULL DEFAULT 0,
    `Estado`            INT            NOT NULL DEFAULT 1,
    `PuntoVenta`        VARCHAR(40)    NULL,
    `NumeroComprobante` VARCHAR(40)    NULL,
    `VendedorId`        INT            NULL,
    `Vendedor`          VARCHAR(120)   NOT NULL,
    `Cae`               VARCHAR(30)    NULL,
    `CaeVencimiento`    DATE           NULL,
    `Subtotal`          DECIMAL(18,2)  NOT NULL DEFAULT 0.00,
    `DescuentoTotal`    DECIMAL(18,2)  NOT NULL DEFAULT 0.00,
    `IvaTotal`          DECIMAL(18,2)  NOT NULL DEFAULT 0.00,
    `Total`             DECIMAL(18,2)  NOT NULL DEFAULT 0.00,
    `Observaciones`     VARCHAR(1000)  NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Ventas_ClienteId` (`ClienteId`),
    KEY `IX_Ventas_VendedorId` (`VendedorId`),
    KEY `IX_Ventas_Fecha` (`Fecha`),
    CONSTRAINT `FK_Ventas_Clientes_ClienteId`
        FOREIGN KEY (`ClienteId`) REFERENCES `Clientes` (`Id`)
        ON DELETE RESTRICT,
    CONSTRAINT `FK_Ventas_Usuarios_VendedorId`
        FOREIGN KEY (`VendedorId`) REFERENCES `Usuarios` (`Id`)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------------------------------
-- Detalle de ventas
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `DetallesVenta` (
    `Id`             INT           NOT NULL AUTO_INCREMENT,
    `VentaId`        INT           NOT NULL,
    `ProductoId`     INT           NOT NULL,
    `ArmaId`         INT           NULL,
    `MunicionLoteId` INT           NULL,
    `Cantidad`       INT           NOT NULL,
    `PrecioUnitario` DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `Descuento`      DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `AlicuotaIva`    DECIMAL(5,2)  NOT NULL DEFAULT 0.00,
    `Subtotal`       DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `Iva`            DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `Total`          DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    PRIMARY KEY (`Id`),
    KEY `IX_DetallesVenta_VentaId` (`VentaId`),
    KEY `IX_DetallesVenta_ProductoId` (`ProductoId`),
    KEY `IX_DetallesVenta_ArmaId` (`ArmaId`),
    KEY `IX_DetallesVenta_MunicionLoteId` (`MunicionLoteId`),
    CONSTRAINT `FK_DetallesVenta_Ventas_VentaId`
        FOREIGN KEY (`VentaId`) REFERENCES `Ventas` (`Id`)
        ON DELETE RESTRICT,
    CONSTRAINT `FK_DetallesVenta_Productos_ProductoId`
        FOREIGN KEY (`ProductoId`) REFERENCES `Productos` (`Id`)
        ON DELETE RESTRICT,
    CONSTRAINT `FK_DetallesVenta_Armas_ArmaId`
        FOREIGN KEY (`ArmaId`) REFERENCES `Armas` (`Id`)
        ON DELETE RESTRICT,
    CONSTRAINT `FK_DetallesVenta_MunicionLotes_MunicionLoteId`
        FOREIGN KEY (`MunicionLoteId`) REFERENCES `MunicionLotes` (`Id`)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------------------------------
-- Movimientos de stock
-- TipoMovimientoStock: 1=Ingreso, 2=EgresoVenta, 3=Ajuste, 4=AnulacionVenta
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `MovimientosStock` (
    `Id`              INT          NOT NULL AUTO_INCREMENT,
    `Fecha`           DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `ProductoId`      INT          NOT NULL,
    `VentaId`         INT          NULL,
    `ArmaId`          INT          NULL,
    `MunicionLoteId`  INT          NULL,
    `Tipo`            INT          NOT NULL,
    `Cantidad`        INT          NOT NULL,
    `StockResultante` INT          NOT NULL,
    `Observacion`     VARCHAR(500) NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_MovimientosStock_ProductoId` (`ProductoId`),
    KEY `IX_MovimientosStock_VentaId` (`VentaId`),
    KEY `IX_MovimientosStock_ArmaId` (`ArmaId`),
    KEY `IX_MovimientosStock_MunicionLoteId` (`MunicionLoteId`),
    CONSTRAINT `FK_MovimientosStock_Productos_ProductoId`
        FOREIGN KEY (`ProductoId`) REFERENCES `Productos` (`Id`)
        ON DELETE RESTRICT,
    CONSTRAINT `FK_MovimientosStock_Ventas_VentaId`
        FOREIGN KEY (`VentaId`) REFERENCES `Ventas` (`Id`)
        ON DELETE RESTRICT,
    CONSTRAINT `FK_MovimientosStock_Armas_ArmaId`
        FOREIGN KEY (`ArmaId`) REFERENCES `Armas` (`Id`)
        ON DELETE RESTRICT,
    CONSTRAINT `FK_MovimientosStock_MunicionLotes_MunicionLoteId`
        FOREIGN KEY (`MunicionLoteId`) REFERENCES `MunicionLotes` (`Id`)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------------------------------
-- Movimientos de proveedores
-- TipoMovimientoProveedor: 1=Deuda, 2=Pago
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `MovimientosProveedor` (
    `Id`            INT           NOT NULL AUTO_INCREMENT,
    `ProveedorId`   INT           NOT NULL,
    `Tipo`          INT           NOT NULL,
    `Fecha`         DATETIME(6)   NOT NULL,
    `Monto`         DECIMAL(18,2) NOT NULL,
    `Observaciones` VARCHAR(500)  NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_MovimientosProveedor_ProveedorId` (`ProveedorId`),
    CONSTRAINT `FK_MovimientosProveedor_Proveedores_ProveedorId`
        FOREIGN KEY (`ProveedorId`) REFERENCES `Proveedores` (`Id`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;

-- -----------------------------------------------------------------------------
-- Verificación
-- -----------------------------------------------------------------------------
SELECT TABLE_NAME, TABLE_ROWS
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = 'StockSantiCaza'
ORDER BY TABLE_NAME;
