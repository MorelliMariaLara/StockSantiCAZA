# Base de datos en DonWeb / Ferozo

La aplicación **no migra ni crea tablas al iniciar**. La base `w400048_santicazarmeria` debe existir y tener el esquema listo antes de publicar.

## Conexión (producción)

En `appsettings.Production.json` (no se sube a Git), en la **misma carpeta** que `StockSantiCaza.Web.dll` dentro de `public_html`.

### Opción A — Ferozo (recomendada, sin contraseña)

El panel suele mostrar cadenas con `Integrated Security` / `Trusted_Connection`. Eso es para la **app publicada en el mismo hosting**: no usa usuario ni contraseña SQL.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sql2016;Database=w400048_santicazarmeria;Integrated Security=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False;Connection Timeout=60"
  },
  "Database": {
    "SkipInitialization": true
  },
  "AllowedHosts": "*"
}
```

Equivalente: `Trusted_Connection=True` en lugar de `Integrated Security=True`.

### Opción B — Usuario SQL (`w400048_MariAdmin`)

Solo si Ferozo te dio o reseteaste la contraseña (correo de alta, “Cambiar clave” en el usuario, o ticket a soporte):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sql2016;Database=w400048_santicazarmeria;User Id=w400048_MariAdmin;Password=TU_PASSWORD;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False;Connection Timeout=60"
  }
}
```

| Parámetro | Valor |
|-----------|-------|
| Servidor | `sql2016` |
| Base | `w400048_santicazarmeria` |
| Usuario SQL (opción B) | `w400048_MariAdmin` |

Si el panel **no deja generar contraseña**, usá la **opción A**. La contraseña de `w400048_MariAdmin` suele servir para conectar **desde tu PC** (con túnel), no para la app en Ferozo.

## Crear o actualizar tablas manualmente

Ejecutá los scripts en el panel SQL de DonWeb o en SSMS conectado a la base:

1. Si la base está **vacía**: crear tablas con los scripts en `scripts/sql/` (consultá `scripts/sql/README.md`).
2. Si la base **ya existía**: `scripts/sql/007-migracion-completa.sql` (idempotente).
3. Para usuario inicial: `scripts/sql/003-limpiar-bd-y-admin-santi.sql` (borra datos; usar con cuidado).

## Publicar

1. `dotnet publish -c Release` o Visual Studio → FolderProfile.
2. Copiá `appsettings.Production.json` a la carpeta publish.
3. Subí todo a `public_html` por FileZilla.
4. Variable en panel: `ASPNETCORE_ENVIRONMENT` = `Production`.

## Si el login falla

- Verificá que la cadena SQL sea correcta y que las tablas existan.
- Probá `https://tudominio.com/api/auth/me` (debe responder JSON 401, no timeout).
- Activá logs en `web.config` (`stdoutLogEnabled="true"`) y revisá `logs/stdout_*.log`.
