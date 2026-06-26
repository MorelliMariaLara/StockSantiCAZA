# Publicar en DonWeb / Ferozo (SQL Server)

## Datos de tu base (panel Ferozo)

| Parámetro | Valor |
|-----------|-------|
| Servidor | `sql2016` |
| Base de datos | `w400048_santicazarmeria` |
| Usuario SQL | `MariAdmin` |
| Contraseña | *(la configurada en el panel)* |

> **Importante:** Las cadenas con `Integrated Security` / `Trusted_Connection` solo funcionan en Windows local. En Ferozo hay que usar **usuario y contraseña SQL**.

## Cadena de conexión para .NET (EF Core)

```text
Server=sql2016;Database=w400048_santicazarmeria;User Id=MariAdmin;Password=TU_PASSWORD;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False;Connection Timeout=60
```

Si DonWeb te muestra el usuario con prefijo de cuenta, probá también:

```text
User Id=w400048_MariAdmin
```

## Configuración en el proyecto

1. Copiá el ejemplo de producción:

```bash
cp src/StockSantiCaza.Web/appsettings.Production.example.json src/StockSantiCaza.Web/appsettings.Production.json
```

2. Editá `appsettings.Production.json` y reemplazá `__PASSWORD__` por tu contraseña.

3. El archivo `appsettings.Production.json` **no se sube a Git** (está en `.gitignore`).

## Publicar la aplicación

```bash
cd src/StockSantiCaza.Web
dotnet publish -c Release -o ./publish
```

Subí el contenido de la carpeta `publish` al hosting por FTP o el administrador de archivos de Ferozo.

## Variables de entorno en Ferozo (alternativa)

En el panel del sitio, podés definir:

| Variable | Valor |
|----------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | *(cadena completa de arriba)* |

## Primera ejecución / tablas

Al iniciar la app, `DbInitializer` ejecuta:

- `EnsureCreated` (crea tablas si la base está vacía)
- Script de tablas `Proveedores` si faltan

Si la base ya tiene datos de SQL Server local, migrá el esquema con los scripts en `scripts/sql/` (SQL Server) o exportá/importá datos.

## Probar conexión desde tu PC

Si `sql2016` no resuelve fuera del hosting, en el panel Ferozo buscá el **host externo** de SQL Server (a veces es una IP o `sql2016.tudominio.com`) y usalo en lugar de `sql2016`.

## Seguridad

- No subas contraseñas al repositorio.
- Cambiá la contraseña si se expuso en algún chat o commit.
- Usá HTTPS en el sitio publicado.
