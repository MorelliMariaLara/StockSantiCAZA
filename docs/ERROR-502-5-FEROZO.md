# Error 502.5 en Ferozo — ANCM Out-Of-Process Startup Failure

## El mensaje

```text
HTTP Error 502.5 - ANCM Out-Of-Process Startup Failure
```

Significa: IIS **intentó** arrancar `StockSantiCaza.Web.exe` pero el proceso **se cerró al instante** o no pudo escuchar el puerto.

---

## Causas más frecuentes en DonWeb/Ferozo

| Causa | Solución |
|-------|----------|
| Subiste solo el `.exe` o solo `wwwroot` | Subí **toda** la carpeta `Publish` (cientos de archivos) |
| Pool IIS **32 bits** y publicaste **win-x64** | Usá `.\scripts\publicar-donweb.ps1` (ahora es **win-x86**) |
| Pool **64 bits** y publicaste x86 | Usá `.\scripts\publicar-donweb-x64.ps1` |
| Falta `appsettings.Production.json` | Copialo al publish y subilo al servidor |
| Carpeta `logs` sin permiso de escritura | Creá `logs` en `public_html` con permiso de escritura |

---

## Pasos para corregir

### 1. Republicar (32 bits — recomendado para Ferozo)

```powershell
git pull
.\scripts\publicar-donweb.ps1
```

El script verifica que existan `.exe`, `web.config` y suficientes `.dll`.

### 2. Subir TODO por FileZilla

En `public_html` deben quedar **todos** los archivos de `Desktop\Publish`:

- `StockSantiCaza.Web.exe`
- `web.config`
- `appsettings.Production.json`
- **Todas** las `.dll` (más de 50)
- Carpetas `wwwroot\` y `logs\`

**Error típico:** subir solo 10–20 archivos. El publish completo tiene **200+ archivos**.

### 3. Reiniciar el sitio en el panel DonWeb

### 4. Leer el log en el servidor

Descargá por FTP:

```text
public_html\logs\stdout_*.log
```

Ahí aparece el error real (arquitectura incorrecta, DLL faltante, etc.).

### 5. Probar

```text
https://santicazastock.com.ar/api/health
```

---

## Si sigue 502.5

1. Probá **64 bits**: `.\scripts\publicar-donweb-x64.ps1` y volvé a subir todo.
2. Contactá DonWeb: preguntá si el application pool es **32 o 64 bits**.
3. Pedí que instalen **ASP.NET Core 6.0 Hosting Bundle** (opcional con publish autocontenido).

---

## Evolución de errores (referencia)

| Error | Significado |
|-------|-------------|
| Timeout en login | Backend no corre |
| **500.31** | IIS sin runtime (modo inprocess) |
| **502.5** | El `.exe` no arrancó (archivos incompletos o arquitectura incorrecta) |
| JSON en `/api/health` | ✅ Funciona |
