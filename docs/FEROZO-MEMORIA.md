# Memoria en Ferozo (plan compartido Windows)

Límite del App Pool en hosting compartido:

| Recurso | Límite |
|---------|--------|
| Memoria privada | 154 MB |
| Memoria virtual | 1.8 GB |

Si la app supera el límite, IIS recicla el proceso → errores 503 y fallos de conexión SQL.

## Optimizaciones aplicadas en el código

- **GC Workstation** (`ServerGarbageCollection=false`) — menos RAM en procesos pequeños
- **Límite de heap** ~90 MB vía `DOTNET_GCHeapHardLimit` en `web.config`
- **DbContext pooled** + consultas con `AsSplitQuery`
- **Reportes/dashboard**: agregados en SQL, sin cargar todas las ventas en memoria
- **Clientes/proveedores**: listados con proyección SQL (sin `Include` masivos)
- **Proveedores**: movimientos se cargan solo al abrir la cuenta
- **Ventas**: máximo 200 registros por consulta
- **Excel**: export máximo 90 días
- **Import stock**: guardado en lotes de 50 filas
- **Sesión**: timeout 2 h (antes 8 h)

## Recomendaciones de uso

1. Evitar exportar Excel de rangos muy largos
2. No importar archivos Excel enormes de una sola vez
3. Si el negocio crece, considerar **CloudServer** en DonWeb (más RAM)

## Publicar

Republicar con FolderProfile y subir todo `publish/` a `public_html`, incluyendo el `web.config` actualizado.
