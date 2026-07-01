# Publicar StockSantiCAZA

Guía unificada para **desarrollo local** y **Ferozo/DonWeb**.

## Desarrollo local

### Requisitos

- .NET 6 SDK
- SQL Server (local o Docker)

### Configuración

1. Copiá `appsettings.Local.json.example` → `appsettings.Local.json` si necesitás otra conexión.
2. Por defecto, `appsettings.Development.json` usa `LARA-NB\SQLEXPRESS02`.
3. En Development, la app **crea el esquema y usuario admin** automáticamente (`admin` / `Admin123!`).

### Ejecutar

```bash
cd src/StockSantiCaza.Web
dotnet run
```

Abrí: `https://localhost:53095/login`

### Verificar

- `GET /api/health` → `{ "status": "ok" }`
- `GET /api/health/db` → base conectada
- Login → dashboard en `/` → navegá por Clientes, Stock, Ventas

---

## Publicar en Ferozo

### 1. Crear configuración de producción

Copiá `appsettings.Production.example.json` → `appsettings.Production.json`  
y poné la contraseña real de SQL.

### 2. Publicar

**Visual Studio:** clic derecho en el proyecto → Publicar → **FolderProfile**  
(salida en carpeta `publish/` del repo)

**O PowerShell:**

```powershell
.\scripts\publicar-donweb.ps1
```

### 3. Subir por FTP

Subí **todo** el contenido de `publish/` a `public_html`:

- `StockSantiCaza.Web.dll` y demás DLLs
- `web.config`
- `wwwroot/` completo
- `appsettings.Production.json`
- Carpetas vacías `logs/` y `keys/` (se llenan en el servidor)

**No subas solo HTML.** La API .NET debe estar en ejecución.

### 4. Base de datos en DonWeb

La BD en `sql2016` debe existir con scripts en `scripts/sql/`.  
En producción `Database:SkipInitialization` está en `true` (sin migración automática).

### 5. Verificar en el servidor

| URL | Esperado |
|-----|----------|
| `/api/health` | JSON `status: ok` |
| `/api/health/db` | JSON `database: connected` |
| `/login` | Formulario de login |
| Login admin | Dashboard en `/` |

---

## Flujo de la aplicación

```
/login  →  ingreso con usuario y contraseña

Administrador  →  / (dashboard) + todos los módulos:
                  Dashboard, Ventas, Clientes, Stock, Proveedores, Reportes, Usuarios

Vendedor       →  /ventas/nueva + módulos operativos:
                  Nueva venta, Historial, Clientes, Stock, Proveedores
                  (sin Dashboard, Reportes ni Usuarios)
```

La sesión usa cookie `StockSanti.Session` (8 horas).

---

## Problemas frecuentes

| Síntoma | Causa | Solución |
|---------|-------|----------|
| Login no responde / timeout | Solo se subió wwwroot | Publicar y subir la app .NET completa |
| Vuelve al login tras entrar | Cookie de sesión no persiste | Subir carpeta `keys/`, no borrarla en republicaciones |
| Error 500 al iniciar | Connection string incorrecta | Revisar `appsettings.Production.json` |
| `api is not defined` | Falta `wwwroot/js/api.js` | Subir `wwwroot/js/` completo |
