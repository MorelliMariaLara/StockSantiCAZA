# Panel Ferozo / DonWeb — dónde mirar cada cosa

Guía visual para la cuenta **w400048** y el sitio **santicazastock.com.ar**.

---

## 1. Entrar al panel

1. Abrí **https://ferozo.com** (o el enlace de DonWeb que uses).
2. Iniciá sesión con tu usuario del hosting (no es el usuario SQL).
3. Entrá a **Mi cuenta** → sitio **w400048** o el dominio **santicazastock.com.ar**.

---

## 2. Datos de la base SQL (usuario y contraseña)

Ruta habitual en el panel:

**Bases de datos** → **SQL Server** → base **`w400048_santicazarmeria`**

Ahí ves:

| Campo en el panel | Valor tuyo |
|-------------------|------------|
| Servidor | `sql2016` |
| Base de datos | `w400048_santicazarmeria` |
| Usuario SQL | `w400048_MariAdmin` |
| Contraseña | la que configuraste (`SantiagoFerreyra@22`) |

También puede aparecer como **Administrador SQL** o **Usuario de base de datos**.

> El usuario FTP `w400048@w400048.ferozo.com` **no** es el usuario de la base.

---

## 3. Variables de entorno (importante para la app)

Algunos planes Ferozo permiten variables para ASP.NET. Si existe una cadena vieja, **pisa** tu `appsettings.Production.json`.

Buscá en el panel (el nombre varía según el plan):

- **Configuración del sitio** → **Variables de entorno**
- **ASP.NET** → **Variables**
- **Aplicaciones** → tu dominio → **Configuración**
- **IIS** / **Hosting Windows** → variables del sitio

### Qué buscar

Si existe alguna de estas, revisala o **borrala** si está mal:

| Nombre de variable | Qué hace |
|--------------------|----------|
| `ASPNETCORE_ENVIRONMENT` | Debe ser `Production` |
| `ConnectionStrings__DefaultConnection` | Cadena SQL completa — **si está mal, rompe la app** |

**Recomendación:** no uses `ConnectionStrings__DefaultConnection` en el panel. Dejá la cadena solo en `appsettings.Production.json` subido por FTP.

### Cómo saber si el panel pisa tu JSON

Abrí en el navegador:

`https://santicazastock.com.ar/api/health`

Si dice `"usaVariableEntorno": true`, hay una variable de entorno activa (panel o `web.config`). Si la cadena es vieja, hay que borrarla o actualizarla.

---

## 4. Tipo de hosting (Windows vs Linux)

Ruta habitual:

**Mi sitio** → **Información** / **Detalles del plan**

Debe decir **Windows** y soportar **ASP.NET Core** / **IIS**.

Si el plan es solo **Linux / PHP**, SQL Server `sql2016` puede no conectar desde la app.

---

## 5. Archivos en el servidor (FTP / FileZilla)

Conectate con FileZilla:

| Campo | Valor |
|-------|-------|
| Servidor | `w400048.ferozo.com` |
| Usuario | `w400048@w400048.ferozo.com` |
| Ruta | `stock.santicazaarmeria.com.ar/public_html` o `public_html` |

### Archivos que tienen que estar juntos (misma carpeta que el `.dll`)

- `StockSantiCaza.Web.dll`
- `appsettings.Production.json` ← contraseña en `Database.SqlPassword`
- `web.config`
- carpeta `wwwroot/`
- carpetas vacías `logs/` y `keys/`

### Logs si algo falla

En FTP: `public_html/logs/stdout_*.log`

---

## 6. Probar después de subir

| URL | Resultado esperado |
|-----|-------------------|
| `/api/health` | `"status": "ok"`, `"sqlServer": "tcp:sql2016,1433"` o `"sql2016"` |
| `/api/health/db` | `"database": "connected"` |
| `/login` | formulario y login funciona |

---

## 7. Si el menú del panel no coincide

DonWeb cambia nombres según el plan. Si no encontrás “Variables de entorno”:

1. Usá el **buscador** del panel con: `SQL`, `variables`, `ASP.NET`.
2. O abrí ticket a soporte: *“¿Dónde configuro variables de entorno para ASP.NET Core en mi plan w400048?”*

Mientras tanto, la app puede funcionar **solo** con `appsettings.Production.json` en `public_html`, sin variables en el panel.
