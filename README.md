# StockSantiCAZA

Aplicación web Blazor WebApp (.NET 8) para control de stock y ventas de armería, con trazabilidad de armas y municiones, validación de CLU, facturación simulada y exportación Excel.

## Estructura

- `src/StockSantiCaza.Web/Components`: componentes Razor, layout, navegación y páginas.
- `src/StockSantiCaza.Web/Models`: entidades de dominio y enumeraciones.
- `src/StockSantiCaza.Web/Data`: `ApplicationDbContext` para SQL Server con Entity Framework Core.
- `src/StockSantiCaza.Web/Services`: servicios de ventas, reportes y facturación electrónica simulada.

## Configuración

Actualice `ConnectionStrings:DefaultConnection` en `appsettings.json` o variables de entorno antes del despliegue en un dominio `.com.ar`.

```bash
dotnet restore
dotnet ef migrations add InitialCreate --project src/StockSantiCaza.Web
dotnet ef database update --project src/StockSantiCaza.Web
dotnet run --project src/StockSantiCaza.Web
```

La integración AFIP queda encapsulada detrás de `IFacturacionElectronicaService`; la implementación actual genera comprobantes simulados para desarrollo.
