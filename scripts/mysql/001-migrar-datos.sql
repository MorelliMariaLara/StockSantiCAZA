-- =============================================================================
-- StockSantiCAZA - Migración de DATOS desde SQL Server a MySQL
-- Ejecutar DESPUÉS de 000-esquema-completo.sql
-- =============================================================================
--
-- ORDEN DE CARGA (respetar claves foráneas):
--   1. Usuarios
--   2. Productos
--   3. Clientes
--   4. Proveedores
--   5. CredencialesCLU
--   6. Armas
--   7. MunicionLotes
--   8. Ventas
--   9. DetallesVenta
--  10. MovimientosStock
--  11. MovimientosProveedor
--
-- =============================================================================

USE `StockSantiCaza`;

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- -----------------------------------------------------------------------------
-- Opción A: Exportar desde SQL Server con SSMS
-- -----------------------------------------------------------------------------
-- 1. Click derecho en la base StockSantiCaza → Tasks → Export Data
-- 2. Origen: SQL Server Native Client
-- 3. Destino: MySQL (ODBC o proveedor MySQL)
-- 4. Mapear tablas en el orden indicado arriba
--
-- O usar "Generate Scripts" en SSMS solo para datos (INSERT) y adaptar sintaxis.

-- -----------------------------------------------------------------------------
-- Opción B: Consultas de exportación en SQL Server (copiar resultado a CSV/INSERT)
-- -----------------------------------------------------------------------------

/*
-- En SQL Server Management Studio, por cada tabla:

SELECT * FROM dbo.Usuarios ORDER BY Id;
SELECT * FROM dbo.Productos ORDER BY Id;
SELECT * FROM dbo.Clientes ORDER BY Id;
SELECT * FROM dbo.Proveedores ORDER BY Id;
SELECT * FROM dbo.CredencialesCLU ORDER BY Id;
SELECT * FROM dbo.Armas ORDER BY Id;
SELECT * FROM dbo.MunicionLotes ORDER BY Id;
SELECT * FROM dbo.Ventas ORDER BY Id;
SELECT * FROM dbo.DetallesVenta ORDER BY Id;
SELECT * FROM dbo.MovimientosStock ORDER BY Id;
SELECT * FROM dbo.MovimientosProveedor ORDER BY Id;
*/

-- -----------------------------------------------------------------------------
-- Opción C: Plantilla INSERT para MySQL (reemplazar valores)
-- -----------------------------------------------------------------------------

/*
INSERT INTO `Usuarios` (`Id`, `Nombre`, `Login`, `PasswordHash`, `Rol`, `Activo`) VALUES
(1, 'Administrador', 'admin', '<hash_desde_sql_server>', 1, 1);

INSERT INTO `Productos` (`Id`, `Sku`, `Nombre`, `Descripcion`, `Categoria`, `Marca`, `Modelo`, `Calibre`,
    `PrecioUnitario`, `CostoUnitario`, `StockActual`, `StockMinimo`, `Activo`, `CreadoEn`) VALUES
(1, 'SKU-001', 'Producto ejemplo', NULL, 1, NULL, NULL, NULL, 100.00, 50.00, 10, 1, 1, NOW(6));
*/

-- -----------------------------------------------------------------------------
-- Ajustes post-migración de IDs (si importaste con IDs explícitos)
-- -----------------------------------------------------------------------------

ALTER TABLE `Usuarios` AUTO_INCREMENT = 1000;
ALTER TABLE `Productos` AUTO_INCREMENT = 1000;
ALTER TABLE `Clientes` AUTO_INCREMENT = 1000;
ALTER TABLE `Proveedores` AUTO_INCREMENT = 1000;
ALTER TABLE `CredencialesCLU` AUTO_INCREMENT = 1000;
ALTER TABLE `Armas` AUTO_INCREMENT = 1000;
ALTER TABLE `MunicionLotes` AUTO_INCREMENT = 1000;
ALTER TABLE `Ventas` AUTO_INCREMENT = 1000;
ALTER TABLE `DetallesVenta` AUTO_INCREMENT = 1000;
ALTER TABLE `MovimientosStock` AUTO_INCREMENT = 1000;
ALTER TABLE `MovimientosProveedor` AUTO_INCREMENT = 1000;

-- Recalcular AUTO_INCREMENT al máximo Id + 1 (ejecutar tras cargar datos):

SET @sql = (
    SELECT CONCAT('ALTER TABLE `', t.TABLE_NAME, '` AUTO_INCREMENT = ', COALESCE(m.max_id, 0) + 1, ';')
    FROM information_schema.TABLES t
    LEFT JOIN (
        SELECT 'Usuarios' AS tbl, MAX(Id) AS max_id FROM Usuarios
        UNION ALL SELECT 'Productos', MAX(Id) FROM Productos
        UNION ALL SELECT 'Clientes', MAX(Id) FROM Clientes
        UNION ALL SELECT 'Proveedores', MAX(Id) FROM Proveedores
        UNION ALL SELECT 'CredencialesCLU', MAX(Id) FROM CredencialesCLU
        UNION ALL SELECT 'Armas', MAX(Id) FROM Armas
        UNION ALL SELECT 'MunicionLotes', MAX(Id) FROM MunicionLotes
        UNION ALL SELECT 'Ventas', MAX(Id) FROM Ventas
        UNION ALL SELECT 'DetallesVenta', MAX(Id) FROM DetallesVenta
        UNION ALL SELECT 'MovimientosStock', MAX(Id) FROM MovimientosStock
        UNION ALL SELECT 'MovimientosProveedor', MAX(Id) FROM MovimientosProveedor
    ) m ON m.tbl = t.TABLE_NAME
    WHERE t.TABLE_SCHEMA = 'StockSantiCaza'
      AND t.TABLE_TYPE = 'BASE TABLE'
      AND m.max_id IS NOT NULL
    LIMIT 1
);

-- Si preferís hacerlo manualmente, por tabla:
-- SELECT MAX(Id) FROM Ventas;
-- ALTER TABLE Ventas AUTO_INCREMENT = <max+1>;

SET FOREIGN_KEY_CHECKS = 1;

-- -----------------------------------------------------------------------------
-- Diferencias SQL Server → MySQL a tener en cuenta
-- -----------------------------------------------------------------------------
-- | SQL Server        | MySQL              |
-- |-------------------|--------------------|
-- | bit               | TINYINT(1)         |
-- | nvarchar(n)       | VARCHAR(n) utf8mb4 |
-- | datetime2         | DATETIME(6)        |
-- | date              | DATE               |
-- | decimal(18,2)     | DECIMAL(18,2)      |
-- | IDENTITY          | AUTO_INCREMENT     |
-- | GETDATE()         | NOW() / CURRENT_TIMESTAMP |
--
-- Los enums de la app se guardan como INT (mismos valores numéricos).
