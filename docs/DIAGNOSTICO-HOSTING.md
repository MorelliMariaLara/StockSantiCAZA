# Diagnóstico de hosting — DonWeb / Ferozo

Guía para verificar que el **backend .NET** está en ejecución y que el frontend puede hablar con la API.

---

## Arquitectura de StockSantiCAZA en producción

| Componente | Dónde corre |
|------------|-------------|
| HTML / CSS / JS | Carpeta `wwwroot/` en `public_html` |
| API REST (`/api/*`) | Mismo proceso .NET (no hay puerto aparte) |
| Hosting | **IIS** en Windows con **ASP.NET Core Module** |
| Base de datos | SQL Server `sql2016` (mismo datacenter DonWeb) |

El frontend llama a rutas relativas (`/api/auth/login`, etc.) en el **mismo dominio y puerto** (443 HTTPS). No hace falta abrir un puerto extra para la API.

---

## Checklist rápido (en orden)

### 1. ¿Responde el proceso .NET?

Abrí en el navegador:

```text
https://TU-DOMINIO/api/health
```

| Resultado | Significado |
|-----------|-------------|
| JSON `{"status":"ok",...}` | El backend .NET **está corriendo** |
| Timeout / página en blanco | IIS no arrancó la app o falta el publish completo |
| 404 HTML genérico | Solo subiste `wwwroot`; faltan `.dll` y `web.config` |

### 2. ¿Conecta a la base SQL?

```text
https://TU-DOMINIO/api/health/db
```

| Resultado | Significado |
|-----------|-------------|
| JSON `database: connected` | Cadena SQL correcta y base accesible |
| `missing_connection_string` | Falta `appsettings.Production.json` en el servidor |
| `exception` / timeout SQL | Contraseña incorrecta, base inexistente o permisos |

### 3. ¿Funciona la API de autenticación?

```text
https://TU-DOMINIO/api/auth/me
```

Debe devolver **JSON 401** (no autorizado). Eso confirma sesión + controladores.

---

## 1. Servidor de hosting — IIS y el proceso .NET

En Ferozo la app corre bajo **IIS**, no como servicio Windows manual ni Docker.

### Archivos obligatorios en `public_html`

- [ ] `web.config`
- [ ] `StockSantiCaza.Web.dll`
- [ ] Todas las demás `.dll`
- [ ] `appsettings.json`
- [ ] `appsettings.Production.json` (con contraseña real)
- [ ] Carpeta `wwwroot/`
- [ ] Carpeta `logs/` (crearla vacía; IIS escribe ahí)

### Reiniciar la aplicación

En el panel DonWeb/Ferozo:

1. Buscá **Reiniciar aplicación** o **Reciclar pool** del sitio.
2. Volvé a probar `/api/health`.

### Si IIS no arranca la app

Causas frecuentes:

| Causa | Solución |
|-------|----------|
| Falta `web.config` | Republicar y subir todo el publish |
| Plan sin .NET 6 | Verificar en panel que el hosting sea **Windows + ASP.NET Core** |
| `dotnet` no en PATH del servidor | Contactar soporte DonWeb |
| Error al iniciar (cadena SQL vacía) | Subir `appsettings.Production.json` |

El `web.config` del proyecto ya tiene `stdoutLogEnabled="true"` para escribir logs en `logs/stdout_*.log`.

---

## 2. Logs — excepciones y base de datos

### Logs de arranque (IIS)

1. Por FTP, creá la carpeta `logs` dentro de `public_html` (si no existe).
2. Reiniciá el sitio desde el panel.
3. Descargá el archivo más reciente: `logs/stdout_YYYYMMDD_HHMMSS_*.log`.

Buscá líneas como:

```text
[StockSantiCAZA] Entorno: Production
[StockSantiCAZA] Server=sql2016
[StockSantiCAZA] Aplicación iniciada...
```

Errores típicos en el log:

| Mensaje | Causa |
|---------|-------|
| `Connection string 'DefaultConnection' was not configured` | Falta `appsettings.Production.json` |
| `Login failed for user 'w400048_MariAdmin'` | Contraseña SQL incorrecta |
| `Cannot open database "w400048_santicazarmeria"` | Base no creada en el panel |
| `Invalid object name 'Usuarios'` | Tablas no creadas (ejecutar scripts SQL) |

### Logs desde el panel DonWeb

En el panel buscá **Registros**, **Logs de errores** o **Estadísticas del sitio** según la versión del panel.

---

## 3. Puertos y firewall

### Mismo origen (tu caso)

El JavaScript usa `fetch('/api/...')` — misma URL que la página:

```text
https://stock.santicazaarmeria.com.ar/login   → HTML
https://stock.santicazaarmeria.com.ar/api/health → API
```

Puertos estándar:

| Puerto | Uso |
|--------|-----|
| **443** | HTTPS (producción) |
| **80** | HTTP (redirige a HTTPS) |

**No** necesitás abrir puertos adicionales en el firewall para la API.

### Si ves timeout en el login

| Síntoma | Causa probable |
|---------|----------------|
| HTML carga, API hace timeout | Backend .NET no corre (solo archivos estáticos) |
| `/api/health` OK, `/api/health/db` falla | Problema SQL, no de puertos |
| Todo timeout | Dominio/DNS o sitio caído en el hosting |

---

## 4. Configuración de producción

### `appsettings.Production.json` en el servidor

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sql2016;Database=w400048_santicazarmeria;User Id=w400048_MariAdmin;Password=TU_PASSWORD;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False;Connection Timeout=60"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Variable de entorno (panel Ferozo)

| Variable | Valor |
|----------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |

---

## 5. Publicación correcta (FileZilla)

1. Visual Studio → **Publicar** → **FolderProfile**.
2. Copiá `appsettings.Production.json` a la carpeta publish.
3. Subí **todo** el contenido de publish a `public_html`.
4. Creá carpeta `logs` en el servidor.
5. Probá `/api/health` → `/api/health/db` → login.

Ver también: [FTP-PUBLICACION.md](./FTP-PUBLICACION.md) y [DONWEB-BASE-DE-DATOS.md](./DONWEB-BASE-DE-DATOS.md).

---

## Resumen para soporte DonWeb

Si contactás soporte, indicá:

- Hosting **Windows** con **ASP.NET Core / .NET 6**
- Sitio en `public_html` con `web.config` y `StockSantiCaza.Web.dll`
- `/api/health` no responde (o adjuntá `logs/stdout_*.log`)
- Base SQL: `sql2016` / `w400048_santicazarmeria` / usuario `w400048_MariAdmin`
