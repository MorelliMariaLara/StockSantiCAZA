# Scripts SQL - StockSantiCAZA

## Base nueva

Si la base **no existe** o está vacía, al publicar en Ferozo la app crea el esquema al primer arranque. Ver [DONWEB-BASE-DE-DATOS.md](../docs/DONWEB-BASE-DE-DATOS.md).

## Base existente (actualizar esquema)

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
