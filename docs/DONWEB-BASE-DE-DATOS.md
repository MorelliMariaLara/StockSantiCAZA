# Base de datos en DonWeb / Ferozo

La aplicación **no migra ni crea tablas al iniciar**. La base `w400048_santicazarmeria` debe existir y tener el esquema listo antes de publicar.

## Conexión (producción)

En `appsettings.Production.json` (no se sube a Git):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sql2016;Database=w400048_santicazarmeria;User Id=w400048_MariAdmin;Password=TU_PASSWORD;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False;Connection Timeout=60"
  }
}
```

Copiá desde `appsettings.Production.example.json` y reemplazá `__PASSWORD__`.

| Parámetro | Valor |
|-----------|-------|
| Servidor | `sql2016` |
| Base | `w400048_santicazarmeria` |
| Usuario | `w400048_MariAdmin` |

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
- Guía completa de diagnóstico: [DIAGNOSTICO-HOSTING.md](./DIAGNOSTICO-HOSTING.md)
- Probá `https://tudominio.com/api/health` (debe dar JSON `status: ok`).
- Probá `https://tudominio.com/api/health/db` (debe dar `database: connected`).
- Probá `https://tudominio.com/api/auth/me` (debe responder JSON 401, no timeout).
- Revisá `logs/stdout_*.log` en el servidor (carpeta `logs` en `public_html`).
