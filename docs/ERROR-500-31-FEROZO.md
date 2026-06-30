# Error 500.31 en Ferozo / DonWeb

## El mensaje

```text
HTTP Error 500.31 - Failed to load ASP.NET Core runtime
The specified version of Microsoft.NetCore.App or Microsoft.AspNetCore.App was not found.
```

## Por qué pasa

IIS intenta cargar la app en modo **inprocess** y busca el **ASP.NET Core Hosting Bundle 6.0** instalado en el servidor. En muchos planes compartidos de Ferozo **no está** o no coincide con tu versión.

No es un error de tu código ni de la contraseña SQL: **la app ni siquiera arranca**.

---

## Solución (pasos)

### 1. Actualizar el código

```powershell
git pull
```

El `web.config` del repo ya trae `hostingModel="outofprocess"`.

### 2. Republicar

```powershell
.\scripts\publicar-donweb.ps1
```

### 3. Subir TODO por FileZilla

Destino: `public_html` — **sobrescribir** archivos viejos.

Verificá que existan:

- `StockSantiCaza.Web.exe`
- `web.config` (con `outofprocess`)
- `appsettings.Production.json`
- carpeta `logs\` (vacía, con permiso de escritura)

### 4. Reiniciar en el panel DonWeb

Reciclar aplicación / pool del sitio.

### 5. Probar

```text
https://santicazastock.com.ar/api/health
```

Debe devolver JSON, no la página de error 500.31.

---

## Si sigue fallando: probar versión 32 bits

Algunos hostings usan pool **x86**:

```powershell
Remove-Item "$env:USERPROFILE\Desktop\Publish" -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish src\StockSantiCaza.Web\StockSantiCaza.Web.csproj -c Release -r win-x86 --self-contained true -o "$env:USERPROFILE\Desktop\Publish"
copy src\StockSantiCaza.Web\appsettings.Production.json "$env:USERPROFILE\Desktop\Publish\"
```

Subí de nuevo y probá `/api/health`.

---

## Revisar logs

En `public_html\logs\stdout_*.log` buscá la línea de error real si la app intentó arrancar.

---

## Contactar DonWeb

Pedí confirmación de:

- Hosting **Windows** con **ASP.NET Core**
- Si el application pool es **32 o 64 bits**
- Si pueden instalar **ASP.NET Core 6.0 Hosting Bundle** (opcional si usás `outofprocess` autocontenido)
