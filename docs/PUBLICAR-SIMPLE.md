# Publicar en DonWeb — un solo paso

## Importante: no alcanza con subir solo el login

El formulario de login **ya usa la API** (`/api/auth/login`). Si subís solo `login.html` y `js/`:

- La pantalla de login **se ve**
- Al presionar Ingresar **falla** (timeout) porque no hay backend .NET

**HTML + API van juntos** en el mismo publish. No hay forma de evitar subir las `.dll` en Ferozo.

---

## Publicación en 3 pasos

### 1. Crear `appsettings.Production.json` (una sola vez)

```bash
copy src\StockSantiCaza.Web\appsettings.Production.example.json src\StockSantiCaza.Web\appsettings.Production.json
```

Editá y poné tu contraseña SQL real.

### 2. Publicar (Visual Studio o script)

**Opción A — Visual Studio**

1. Clic derecho en **StockSantiCaza.Web** → **Publicar**
2. Perfil **FolderProfile**
3. Publicar

**Opción B — Script automático (Windows)**

```powershell
.\scripts\publicar-donweb.ps1
```

Genera la carpeta `C:\Users\Maria Lara\Desktop\Publish` lista para FileZilla.

### 3. Subir TODO por FileZilla

| Origen (tu PC) | Destino (Ferozo) |
|----------------|------------------|
| Todo el contenido de `Publish\` | `public_html\` |

**Checklist en `public_html`:**

- [ ] `web.config`
- [ ] `StockSantiCaza.Web.dll`
- [ ] Todas las `.dll`
- [ ] `appsettings.Production.json`
- [ ] Carpeta `wwwroot\` (login, css, js)
- [ ] Carpeta `logs\`

---

## Probar que funciona (en este orden)

```text
1. https://TU-DOMINIO/api/health          → JSON status ok
2. https://TU-DOMINIO/api/health/db       → database connected
3. https://TU-DOMINIO/login               → formulario de login
4. Ingresar usuario y contraseña            → entra al sistema
```

Si el paso 1 falla, el problema **no es el login**: es que falta el backend .NET en el servidor.

---

## ¿Por qué Chrome dice "Sitio peligroso"?

Eso es independiente de la app. El dominio `santicazastock.com.ar` está marcado por Google Safe Browsing.

La API puede funcionar (comprobamos `/api/health`) pero Chrome bloquea la página antes de mostrarla.

**Solución:** limpiar archivos raros en `public_html`, verificar SSL en DonWeb y pedir revisión en Google Search Console.

Ver [DIAGNOSTICO-HOSTING.md](./DIAGNOSTICO-HOSTING.md).

---

## Resumen

| Lo que querés | Lo que hay que hacer |
|---------------|----------------------|
| "Publicar como el login" | Subir **todo** el publish (HTML + API juntos) |
| Solo HTML | ❌ No funciona el login ni ningún módulo |
| Todo por API | ✅ Ya está: login, stock, ventas, etc. usan `/api/*` |
| Un solo paso | Usar `scripts/publicar-donweb.ps1` + FileZilla |
