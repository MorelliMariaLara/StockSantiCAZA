# Ferozo: límite de memoria (154 MB)

## Qué dice soporte

Si el App Pool supera **154 MB de memoria privada**, IIS **recicla el proceso** → el sitio queda cargando, error 503, o no entra al login.

**Local funciona** porque tu PC no tiene ese límite. **Ferozo** sí.

## Optimizaciones aplicadas en el código

- Autenticación por **cookie cifrada** (sin sesión en RAM)
- DbContext pool reducido a **8** en producción
- Sin compresión HTTP en producción
- Consultas SQL livianas, sin cargar datos masivos
- GC workstation (`DOTNET_gcServer=0`)
- TCP forzado: `tcp:sql2016,1433`
- Contraseña SQL en `Database.SqlPassword` (soporta `@`)

## Realidad del plan compartido

ASP.NET Core 6 + SQL Server en **154 MB** va muy justo. Si después de republicar sigue reciclando:

**Opción recomendada por Ferozo:** migrar a **CloudServer** (más RAM).

## Publicar correctamente

1. `appsettings.Production.json` con `SqlPassword` (no en Git)
2. FolderProfile → subir todo `publish/` a `public_html`
3. `web.config` **sin** `ConnectionStrings__DefaultConnection`
4. Carpeta `keys/` en el servidor (no borrar entre publicaciones)
5. Probar `/api/health` luego `/api/health/db`

## Login en Ferozo

- Usuario app: `Santi.F` / `Santicaza`
- Si la página carga pero login falla → problema de SQL (cadena o red)
- Si la página **no carga nada** → App Pool reciclado por memoria (esperar 1 min o CloudServer)

## Error 502.5 (ANCM Startup Failure)

Si el navegador muestra **HTTP Error 502.5**, la app .NET **no llegó a arrancar** (no es un error de login).

1. En el panel Ferozo → **Administrador de archivos** → `public_html/logs/` → abrir el último `stdout_*.log`
2. Buscar líneas `[StockSantiCAZA] ERROR` al inicio del archivo
3. Verificar que existan en `public_html`:
   - `StockSantiCaza.Web.dll`
   - `web.config` (sin `ConnectionStrings__DefaultConnection` vieja)
   - `appsettings.Production.json` con `Database.SqlPassword`
   - carpetas `keys/` y `logs/` con permiso de escritura
4. Si el log no muestra nada o el archivo está vacío → el App Pool se cayó por **memoria (154 MB)** antes de escribir; esperar 1 minuto y reintentar, o migrar a CloudServer
