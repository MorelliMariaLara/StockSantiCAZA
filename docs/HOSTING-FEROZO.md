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

---

## ¿El dominio inactivo impide publicar?

**En general, no.** Son cosas distintas:

| Problema | Síntoma | Relacionado al dominio |
|----------|---------|----------------------|
| **Fallo FTP** | Visual Studio dice error al subir archivos | No |
| **Sitio no abre** | Publicación OK pero `santicazastock.com.ar` no carga | Sí (DNS, propagación, SSL) |
| **Error 500 al entrar** | El dominio abre pero la app crashea | No (config BD / .NET en servidor) |

El FTP usa la **IP del servidor** (`200.58.120.140`) o `w400048.ferozo.com`, no depende de que el dominio esté propagado.

Si Visual Studio muestra error **después** de publicar al abrir el navegador, puede ser solo el dominio. Revisá en Ferozo → Administrador de archivos si los archivos llegaron a `public_html`.

---

## Requisitos del plan Donweb

Tu app es **ASP.NET Core 6 + SQL Server**. Necesitás **Web Hosting Windows** (no Linux). En el panel Ferozo deberías ver soporte para **.NET Core** y **MS SQL Server 2016**.

Si tu plan es solo Linux/PHP, la publicación FTP puede funcionar pero **la app no va a ejecutarse**.

---

## Publicar paso a paso (Visual Studio)

### Antes de publicar

1. Creá `appsettings.Production.json` (copiá del `.example`) con `Server=sql2016` y tu contraseña.
2. Verificá en Ferozo → FTP: usuario, servidor, contraseña (generá una nueva si hace falta).
3. Confirmá que la ruta remota es `/public_html` (Administrador de archivos).

### Opción A — Perfil FTPProfile

1. Clic derecho en el proyecto → **Publicar** → perfil `FTPProfile`.
2. Si falla la contraseña, editá el perfil y volvé a ingresarla.
3. **Modo pasivo** ya está activado (`FtpPassiveMode`).

### Opción B — Carpeta + FileZilla (más confiable)

1. Publicá con perfil `FolderProfile` → genera `publish/ferozo/`.
2. En FileZilla conectá con los datos del panel Ferozo.
3. Subí **todo el contenido** de `publish/ferozo/` dentro de `public_html` (no la carpeta `ferozo` entera).

### Después de publicar

En el panel Ferozo (si está disponible), definí:

- `ASPNETCORE_ENVIRONMENT` = `Production`

---

## Errores frecuentes al publicar

| Error | Qué hacer |
|-------|-----------|
| `530 Login incorrect` | Regenerá contraseña FTP en Ferozo y actualizá el perfil |
| `550 Permission denied` | Verificá ruta `/public_html` y permisos de la cuenta FTP |
| Timeout / muchos archivos | Usá `FolderProfile` + FileZilla |
| Publicó pero sitio en blanco | Falta `web.config` o plan no es Windows/.NET |
| Error 500.30 al entrar | Cadena `Server=sql2016` en producción + `Encrypt=False` |
| Dominio no resuelve | Esperá propagación DNS o probá la URL temporal de Ferozo |

---

## Probar sin esperar al dominio

En el panel Ferozo suele haber una **URL temporal** del hosting (tipo `http://w400048.ferozo.com` o similar). Usala para probar si la app corre antes de que `santicazastock.com.ar` esté activo.
