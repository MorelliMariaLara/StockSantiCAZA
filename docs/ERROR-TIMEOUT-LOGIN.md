# Error: "El servidor no respondió a tiempo" en el login

Si ves el formulario de login pero aparece este mensaje al cargar o al presionar **Ingresar**, el **HTML sí está en el servidor** pero la **API .NET no responde**.

La página llama a `/api/auth/me` y `/api/auth/login`. Sin la aplicación .NET en ejecución, esas rutas no funcionan.

---

## Causa más frecuente

Se subió solo la carpeta `wwwroot` (HTML, CSS, JS) y **no** el publish completo de la aplicación.

| Sí subiste esto | No alcanza |
|-----------------|------------|
| `login.html`, `js/`, `css/` | La API `/api/*` no existe |

---

## Qué hacer en FileZilla (paso a paso)

### 1. Publicar en tu PC

En Visual Studio: clic derecho en **StockSantiCaza.Web** → **Publicar** → perfil **FolderProfile**  
(carpeta: `C:\Users\Maria Lara\Desktop\Publish`)

O en consola:

```bash
cd src\StockSantiCaza.Web
dotnet publish -c Release -o "C:\Users\Maria Lara\Desktop\Publish"
```

### 2. Crear `appsettings.Production.json`

```bash
copy src\StockSantiCaza.Web\appsettings.Production.example.json src\StockSantiCaza.Web\appsettings.Production.json
```

Editá el archivo y reemplazá `__PASSWORD__` por la contraseña SQL del panel Ferozo.  
Copiá ese archivo a la carpeta **Publish** (junto a `web.config`).

### 3. Subir TODO el contenido de Publish

En FileZilla:

1. Panel izquierdo: abrí `C:\Users\Maria Lara\Desktop\Publish`
2. Panel derecho: entrá a `public_html` (o `stock.santicazaarmeria.com.ar/public_html`)
3. Seleccioná **todos** los archivos y carpetas del panel izquierdo
4. Arrastrá al panel derecho (sobrescribir si pregunta)

**Archivos obligatorios en la raíz de `public_html`:**

- `web.config`
- `StockSantiCaza.Web.dll`
- `appsettings.json`
- `appsettings.Production.json`
- Todas las demás `.dll`
- Carpeta `wwwroot/` (con HTML, JS, CSS)

### 4. Configurar el hosting

En el panel DonWeb/Ferozo:

| Variable | Valor |
|----------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |

Alternativa: si no usás `appsettings.Production.json`, definí la variable  
`ConnectionStrings__DefaultConnection` con la cadena SQL completa.

### 5. Verificar que tenés Windows Hosting con .NET 6

Esta aplicación **no funciona** en hosting Linux compartido. Necesitás un plan **Windows** con soporte **ASP.NET Core / .NET 6**.

Consultá en el panel o con soporte DonWeb si tu plan incluye .NET Core.

---

## Prueba rápida en el navegador

Abrí (reemplazá por tu dominio):

```text
https://stock.santicazaarmeria.com.ar/api/auth/me
```

| Resultado | Significado |
|-----------|-------------|
| JSON con error 401 | La API .NET **funciona** (login debería andar) |
| Página HTML genérica / 404 | Solo archivos estáticos; falta publish completo |
| Carga infinita / timeout | App no arranca (revisar `web.config`, .NET 6, cadena SQL) |

---

## Si la API responde pero el login falla

- Usuario por defecto: `admin` / `Admin123!` (minúsculas en el usuario)
- Revisá que la base `w400048_santicazaarmeria` exista y tenga tablas
- Si la base está vacía, descomentá en `Program.cs` la línea `DbInitializer` y volvé a publicar

---

## Habilitar logs en el servidor (diagnóstico)

Editá `web.config` en el servidor y cambiá:

```xml
stdoutLogEnabled="true"
```

Creá la carpeta `logs` en `public_html`. Reiniciá el sitio desde el panel.  
Revisá `logs\stdout_*.log` para ver errores de arranque (cadena SQL, permisos, etc.).
