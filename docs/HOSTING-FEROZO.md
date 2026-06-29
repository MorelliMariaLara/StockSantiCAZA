# Publicar en DonWeb / Ferozo (SQL Server)

## FTP ≠ base de datos

| Qué | Para qué sirve |
|-----|----------------|
| **FTP** (`w400048.ferozo.com`) | Subir los archivos de la aplicación al hosting |
| **SQL Server** (`sql2016`) | La base de datos (solo cuando la app corre **en** Ferozo) |

La app **no** se conecta a la BD por FTP. La cadena de conexión va en `appsettings.Production.json` o en variables de entorno del panel.

---

## Datos de tu base (panel Ferozo)

| Parámetro | Valor |
|-----------|-------|
| Servidor (en el hosting) | `sql2016` |
| Base de datos | `w400048_santicazaarmeria` |
| Usuario SQL | `w400048_MariAdmin` |
| Contraseña | *(la del panel)* |

> En Ferozo el servidor `sql2016` **solo funciona cuando la app está publicada en el mismo hosting**. Desde tu PC no alcanza con poner `sql2016` o `127.0.0.1` sin un túnel SSH.

---

## Escenario A — Desarrollar en tu PC (Visual Studio)

Usá la base **local** (`LARA-NB\SQLEXPRESS02`). Ya está en:

- `appsettings.json`
- `appsettings.Development.json`

Ejecutá con perfil **Development** (F5). No uses la cadena de Donweb en local salvo que tengas túnel SSH (escenario C).

---

## Escenario B — App publicada en Ferozo (producción)

### 1. Crear configuración de producción

```bash
copy src\StockSantiCaza.Web\appsettings.Production.example.json src\StockSantiCaza.Web\appsettings.Production.json
```

Editá `appsettings.Production.json` y reemplazá `__PASSWORD__` por tu contraseña real.

Cadena correcta en el servidor:

```text
Server=sql2016;Database=w400048_santicazaarmeria;User Id=w400048_MariAdmin;Password=TU_PASSWORD;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False;Connection Timeout=60
```

### 2. Publicar

**Opción Visual Studio:** perfil `FTPProfile` → publicar a  
`ftp://w400048.ferozo.com/stock.santicazaarmeria.com.ar/public_html`

**Opción línea de comandos:**

```bash
cd src/StockSantiCaza.Web
dotnet publish -c Release -o ./publish
```

Subí el contenido de `publish/` por FTP (o usá el perfil de publicación).

### 3. Variables en el panel Ferozo (alternativa)

| Variable | Valor |
|----------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | *(cadena completa de arriba)* |

---

## Escenario C — Conectar desde tu PC a la BD de Donweb (opcional)

Solo si tenés un **túnel SSH** activo hacia Ferozo:

1. Abrí el túnel (puerto local 1433 → SQL remoto).
2. Copiá el ejemplo local:

```bash
copy src\StockSantiCaza.Web\appsettings.Local.json.example src\StockSantiCaza.Web\appsettings.Local.json
```

3. Editá `appsettings.Local.json` con tu contraseña.
4. Ese archivo **no se sube a Git** y pisa `appsettings.Development.json`.

Sin túnel activo, `127.0.0.1,1433` siempre falla con *"conexión denegada"*.

**No pongas la cadena de Donweb en `appsettings.Development.json`** — Visual Studio usa Development al presionar F5 y te va a pisar la config local.

---

## Primera ejecución en Ferozo

Al iniciar, `DbInitializer` crea tablas y aplica el script de migración automático.

Si la base ya tiene datos viejos, revisá `scripts/sql/007-migracion-completa.sql`.

---

## Seguridad

- No subas contraseñas al repositorio Git.
- `appsettings.Production.json` está en `.gitignore`.
- Si una contraseña quedó expuesta en un commit, cambiala en el panel de Ferozo.
