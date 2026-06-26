# Scripts SQL - StockSantiCAZA

## Base existente (actualizar)

Si ya tenés la base `StockSantiCaza` creada por la aplicación, ejecutá en **SQL Server Management Studio** o **Azure Data Studio**:

1. `001-proveedores.sql` — crea las tablas `Proveedores` y `MovimientosProveedor` si no existen.

## Consultas de control

2. `002-consultas-proveedores.sql` — saldos por proveedor, total adeudado e historial.

## Base nueva

Si la base **no existe**, no hace falta ejecutar scripts manualmente: al iniciar la app, `EnsureCreated` crea todo el esquema automáticamente (incluido proveedores).

## Conexión

La cadena configurada en `appsettings.json`:

```
Server=LARA-NB\SQLEXPRESS02;Database=StockSantiCaza;Trusted_Connection=True;...
```

Ajustá `USE [StockSantiCaza]` en los scripts si tu base tiene otro nombre.

## Tipos de movimiento

| Tipo | Valor | Efecto        |
|------|-------|---------------|
| Deuda | 1    | Aumenta saldo |
| Pago  | 2    | Reduce saldo  |

**Saldo pendiente** = suma(deudas) − suma(pagos)
