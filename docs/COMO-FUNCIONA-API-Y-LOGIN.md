# Cómo se conectan el login, la API y el backend

## Una sola aplicación (no son dos cosas separadas)

```
Navegador
   │
   ├─ GET /login          → login.html (pantalla)
   ├─ POST /api/auth/login → AuthController (.NET) → SQL Server
   ├─ GET /inicio         → index.html (dashboard)
   ├─ GET /api/reportes/dashboard → ReportesController → SQL
   └─ GET /api/stock/...  → StockController → SQL
```

Todo corre en **el mismo sitio** (`santicazastock.com.ar`). No hay puerto aparte ni segunda URL para la API.

| Parte | Qué es | Dónde está |
|-------|--------|------------|
| **Frontend** | HTML + JS en `wwwroot/` | `login.html`, `stock.html`, `js/api.js` |
| **API / Backend** | Controladores `/api/*` | `Controllers/Api/*.cs` |
| **Base de datos** | SQL Server DonWeb | `sql2016` / `w400048_santicazarmeria` |

El archivo `js/api.js` llama a rutas como `/api/auth/login` en el **mismo dominio**.

---

## Flujo después de loguearse

1. **Login** — `POST /api/auth/login` con usuario y contraseña.
2. **Sesión** — el servidor guarda la sesión en cookie `StockSanti.Session`.
3. **Redirección** — el navegador va a `/inicio` (dashboard).
4. **Cada página** — `app.js` llama `GET /api/auth/me` para verificar la sesión.
5. **Datos** — cada módulo usa la API:
   - Stock → `/api/stock/*`
   - Ventas → `/api/ventas/*`
   - Clientes → `/api/clientes/*`
   - etc.

Todas las peticiones llevan `credentials: 'same-origin'` para enviar la cookie de sesión.

---

## Qué tiene que estar publicado en Ferozo

Si falta **cualquiera** de estos, el login o lo que sigue **no funciona**:

| Archivo / carpeta | Para qué |
|-------------------|----------|
| `StockSantiCaza.Web.exe` | Backend .NET |
| `web.config` | IIS arranca la app |
| `appsettings.Production.json` | Conexión SQL |
| `wwwroot/` | Pantallas HTML y `js/api.js` |
| `keys/` | Sesión estable (cookies) |
| `logs/` | Diagnóstico de errores |

**No es** “subir el login y después conectar la API”: van **juntos** en un solo publish.

---

## Probar la conexión (en orden)

```text
1. https://TU-DOMINIO/api/health        → {"status":"ok"}
2. https://TU-DOMINIO/api/health/db      → {"database":"connected"}
3. https://TU-DOMINIO/api/config         → info de la API
4. https://TU-DOMINIO/login              → formulario
5. Ingresar → debe ir a /inicio y cargar datos
```

Si el paso 1 falla, el backend no corre. Si 1–3 OK pero el login falla, es SQL o usuario. Si el login OK pero `/inicio` vacío, es sesión o tablas vacías.

---

## `appsettings.Production.json` completo

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sql2016;Database=w400048_santicazarmeria;User Id=w400048_MariAdmin;Password=TU_PASSWORD;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False;Connection Timeout=60"
  },
  "Hosting": {
    "DisableHttpsRedirection": true
  }
}
```

---

## Publicar

```powershell
git pull
.\scripts\publicar-donweb.ps1
```

Subir **todo** `Desktop\Publish` a `public_html`.

---

## Si el login entra pero las pantallas no cargan datos

1. Abrí F12 → pestaña **Red** → buscá llamadas a `/api/...` con error 401 o 500.
2. Verificá que exista la cookie `StockSanti.Session` después del login.
3. Revisá `logs/stdout_*.log` en el servidor.
4. Confirmá que la base tenga tablas y usuarios (scripts en `scripts/sql/`).
