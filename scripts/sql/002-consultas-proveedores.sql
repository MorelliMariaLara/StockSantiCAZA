-- =============================================================================
-- StockSantiCAZA - Consultas útiles: Proveedores y saldos
-- =============================================================================

USE [StockSantiCaza];
GO

-- Saldo por proveedor (Deudas - Pagos)
-- Tipo: 1 = Deuda, 2 = Pago
SELECT
    p.Id,
    p.NombreRazonSocial,
    p.Activo,
    ISNULL(SUM(CASE WHEN m.Tipo = 1 THEN m.Monto ELSE 0 END), 0) AS TotalDeudas,
    ISNULL(SUM(CASE WHEN m.Tipo = 2 THEN m.Monto ELSE 0 END), 0) AS TotalPagos,
    ISNULL(SUM(CASE WHEN m.Tipo = 1 THEN m.Monto WHEN m.Tipo = 2 THEN -m.Monto ELSE 0 END), 0) AS SaldoPendiente
FROM dbo.Proveedores p
LEFT JOIN dbo.MovimientosProveedor m ON m.ProveedorId = p.Id
GROUP BY p.Id, p.NombreRazonSocial, p.Activo
ORDER BY SaldoPendiente DESC, p.NombreRazonSocial;
GO

-- Saldo total adeudado (solo proveedores activos con saldo > 0)
SELECT
    SUM(Saldo.SaldoPendiente) AS SaldoTotalAdeudado
FROM (
    SELECT
        p.Id,
        ISNULL(SUM(CASE WHEN m.Tipo = 1 THEN m.Monto WHEN m.Tipo = 2 THEN -m.Monto ELSE 0 END), 0) AS SaldoPendiente
    FROM dbo.Proveedores p
    LEFT JOIN dbo.MovimientosProveedor m ON m.ProveedorId = p.Id
    WHERE p.Activo = 1
    GROUP BY p.Id
) AS Saldo
WHERE Saldo.SaldoPendiente > 0;
GO

-- Historial de movimientos de un proveedor (cambiar @ProveedorId)
DECLARE @ProveedorId int = 1;

SELECT
    m.Fecha,
    CASE m.Tipo WHEN 1 THEN 'Deuda' WHEN 2 THEN 'Pago' ELSE 'Otro' END AS Tipo,
    m.Monto,
    m.Observaciones,
    SUM(CASE WHEN m.Tipo = 1 THEN m.Monto WHEN m.Tipo = 2 THEN -m.Monto ELSE 0 END)
        OVER (ORDER BY m.Fecha, m.Id ROWS UNBOUNDED PRECEDING) AS SaldoAcumulado
FROM dbo.MovimientosProveedor m
WHERE m.ProveedorId = @ProveedorId
ORDER BY m.Fecha, m.Id;
GO

-- Ejemplo: insertar proveedor con deuda inicial manualmente
/*
INSERT INTO dbo.Proveedores (NombreRazonSocial, Telefono, Activo)
VALUES (N'Proveedor Ejemplo S.A.', N'11-5555-1234', 1);

DECLARE @NuevoProveedorId int = SCOPE_IDENTITY();

INSERT INTO dbo.MovimientosProveedor (ProveedorId, Tipo, Fecha, Monto, Observaciones)
VALUES (@NuevoProveedorId, 1, GETDATE(), 1500.00, N'Deuda inicial');
*/

-- Ejemplo: registrar un pago manualmente
/*
INSERT INTO dbo.MovimientosProveedor (ProveedorId, Tipo, Fecha, Monto, Observaciones)
VALUES (1, 2, '2026-06-25', 500.00, N'Pago transferencia');
*/
