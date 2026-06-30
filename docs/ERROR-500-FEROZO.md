# Error HTTP 500 en Ferozo

## El mensaje

```text
Esta página no funciona
HTTP ERROR 500
```

Es un **progreso** respecto a 500.31 y 502.5: IIS ya intenta ejecutar la app, pero algo falla al procesar la petición.

---

## Causa más frecuente (ya corregida en el repo)

**Redirección HTTPS** (`UseHttpsRedirection`) en hosting compartido Ferozo.

IIS ya termina SSL; si la app fuerza otra redirección, suele producir **error 500** o bucles.

**Solución:** en `appsettings.Production.json`:

```json
"Hosting": {
  "DisableHttpsRedirection": true
}
```

(Esta opción ya viene en `appsettings.Production.example.json`.)

---

## Pasos

### 1. Actualizar y republicar

```powershell
git pull
.\scripts\publicar-donweb.ps1
```

### 2. Verificar `appsettings.Production.json` en el servidor

Debe incluir la cadena SQL **y**:

```json
"Hosting": {
  "DisableHttpsRedirection": true
}
```

### 3. Subir todo de nuevo a `public_html`

### 4. Reiniciar sitio en panel DonWeb

### 5. Probar en este orden

```text
https://santicazastock.com.ar/api/health
https://santicazastock.com.ar/login
```

---

## Si sigue el 500

Descargá por FTP el log:

```text
public_html\logs\stdout_*.log
```

Buscá líneas con `fail`, `exception` o `error`.

Errores típicos:

| En el log | Solución |
|-----------|----------|
| `ConnectionStrings` / SQL | Revisar `appsettings.Production.json` |
| `Could not find file wwwroot` | Subir carpeta `wwwroot` completa |
| `Invalid object name 'Usuarios'` | Ejecutar scripts SQL en la base |
| `BadImageFormatException` | Arquitectura incorrecta: probá x86 vs x64 |

---

## Historial de errores IIS

| Error | Significado |
|-------|-------------|
| Timeout | Backend no corre |
| 500.31 | Sin runtime .NET en IIS |
| 502.5 | El `.exe` no arrancó |
| **500** | La app corre pero falla al procesar (HTTPS, SQL, archivos) |
| JSON en `/api/health` | ✅ OK |
