# StockSantiCAZA

Aplicaci?n web para control de stock y ventas de armer?a, con trazabilidad de armas y municiones, validaci?n de CLU y exportaci?n Excel.

## Arquitectura

- **Frontend:** HTML + JavaScript vanilla en `wwwroot/` (misma UI y flujos que el sistema original).
- **Backend:** ASP.NET Core 6 con API REST (`/api/*`) y sesi?n por cookie.
- **Base de datos:** SQL Server con Entity Framework Core.

## Estructura

- `src/StockSantiCaza.Web/wwwroot/`: p?ginas HTML, CSS y JS del frontend.
- `src/StockSantiCaza.Web/Controllers/Api/`: endpoints REST para cada m?dulo.
- `src/StockSantiCaza.Web/Models`: entidades de dominio y enumeraciones.
- `src/StockSantiCaza.Web/Data`: `ApplicationDbContext` para SQL Server.
- `src/StockSantiCaza.Web/Services`: l?gica de negocio (ventas, reportes, stock, auth).

## Rutas

| Ruta | M?dulo |
|------|--------|
| `/` | Dashboard (admin) |
| `/login` | Inicio de sesi?n |
| `/ventas/nueva` | Nueva venta |
| `/ventas` | Historial de ventas |
| `/clientes` | Clientes y CLU |
| `/stock` | Stock e importaci?n Excel |
| `/proveedores` | Proveedores y cuentas |
| `/reportes` | Reportes (admin) |
| `/usuarios` | Usuarios (admin) |

## Configuraci?n

Actualice `ConnectionStrings:DefaultConnection` en `appsettings.json` o variables de entorno antes del despliegue.

```bash
dotnet restore
dotnet run --project src/StockSantiCaza.Web
```

Usuario inicial: `admin` / `Admin123!`

## Notas

Los componentes Blazor en `Components/` se conservan como referencia hist?rica; la aplicaci?n activa usa el frontend HTML en `wwwroot/`.
