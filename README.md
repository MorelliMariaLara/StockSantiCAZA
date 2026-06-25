# StockSantiCAZA

Aplicación web Blazor WebApp (.NET 8) para control de stock y ventas de armería, con trazabilidad de armas y municiones, validación de CLU, facturación simulada y exportación Excel.

## Estructura

- `src/StockSantiCaza.Web/Components`: componentes Razor, layout, navegación y páginas.
- `src/StockSantiCaza.Web/Models`: entidades de dominio y enumeraciones.
- `src/StockSantiCaza.Web/Data`: `ApplicationDbContext` para SQL Server con Entity Framework Core.
- `src/StockSantiCaza.Web/Services`: servicios de ventas, reportes y facturación electrónica simulada.

## Ejecución local (pruebas)

Requisitos:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server LocalDB, SQL Server Express o SQL Server Developer
- Certificado HTTPS de desarrollo de ASP.NET Core

### 1. Base de datos

La cadena por defecto apunta a tu servidor SQL Server Express:

```json
"DefaultConnection": "Server=LARA-NB\\SQLEXPRESS02;Database=StockSantiCAZA;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False"
```

Si necesita otra instancia, copie `appsettings.Local.json.example` a `appsettings.Local.json` y ajuste la cadena.

### Solución de problemas SQL

Si aparece un error sobre `SQLUserInstance.dll` o LocalDB, la aplicación está intentando usar **LocalDB** en lugar de **LARA-NB\\SQLEXPRESS02**.

1. Actualice el código del repositorio.
2. En Visual Studio, abra **Manage User Secrets** y elimine cualquier `ConnectionStrings:DefaultConnection` antigua con `(localdb)`.
3. En **services.msc**, inicie:
   - `SQL Server (SQLEXPRESS02)`
   - `SQL Server Browser` (recomendado para instancias nombradas)
4. Verifique en SSMS que pueda conectarse a `LARA-NB\\SQLEXPRESS02` y que exista la base `StockSantiCAZA`.

### 2. Certificado HTTPS

```bash
dotnet dev-certs https --trust
```

### 3. Levantar la aplicación

Desde la raíz del repositorio:

```bash
dotnet restore
dotnet run --project src/StockSantiCaza.Web --launch-profile https
```

La app quedará disponible en:

- **https://localhost:53095/**
- http://localhost:53096

En Visual Studio, seleccione el perfil `https` y presione F5.

### 4. Datos de prueba

En `Development`, la aplicación aplica migraciones automáticamente y carga datos demo:

- Cliente con CLU vigente y arma registrada calibre `9mm`
- Cliente con CLU vencida (para probar bloqueo de venta)
- Productos generales, armas con serie, munición por lote y alerta de stock mínimo

### 5. Flujo sugerido de prueba

1. Abrir el dashboard y revisar alertas de stock.
2. Ir a **Nueva venta** y vender munición `9mm` al cliente con CLU vigente.
3. Intentar vender arma/munición al cliente con CLU vencida y verificar el bloqueo.
4. Ir a **Reportes**, descargar Excel e imprimir el resumen.

## Publicación

Cuando las pruebas locales estén correctas, actualice la cadena de conexión para el entorno productivo y publique con:

```bash
dotnet publish src/StockSantiCaza.Web -c Release -o ./publish
```

La integración AFIP queda encapsulada detrás de `IFacturacionElectronicaService`; la implementación actual genera comprobantes simulados para desarrollo.
