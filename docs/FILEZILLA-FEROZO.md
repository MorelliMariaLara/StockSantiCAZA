# Conectar FileZilla a Donweb / Ferozo

## Antes de todo

- **No uses SFTP** ni puerto **22** — en Web Hosting compartido Donweb **no está habilitado**.
- **No uses el dominio** `santicazastock.com.ar` si todavía no está activo; usá el **servidor FTP del panel**.
- La contraseña FTP **no se puede ver** en el panel: hay que **generar una nueva**.

---

## Paso 1 — Obtener datos en Ferozo

1. Entrá a https://ferozo.host/login
2. **Mi Sitio Web** → **FTP**
3. Clic en el **ícono del ojo** de tu cuenta FTP
4. Anotá:
   - **Servidor** (host)
   - **Usuario** (completo, ej: `w400048@w400048.ferozo.com`)
5. Escribí una **contraseña nueva** → **Cambiar contraseña**

Si el hosting es nuevo, completá antes el asistente **“Primeros pasos”** en el Área de Cliente Donweb; sin eso a veces FTP no responde.

---

## Paso 2 — Configurar FileZilla (Gestor de sitios)

1. **Archivo** → **Gestor de sitios** → **Nuevo sitio**
2. Completá así:

| Campo | Valor |
|-------|--------|
| **Protocolo** | `FTP - Protocolo de transferencia de archivos` |
| **Host** | El que muestra Ferozo (ej: `w400048.ferozo.com` o `200.58.120.140`) |
| **Puerto** | `21` |
| **Cifrado** | `Requerir FTP explícito sobre TLS` |
| **Tipo de acceso** | Normal |
| **Usuario** | Completo, ej: `w400048@w400048.ferozo.com` |
| **Contraseña** | La que acabás de crear en Ferozo |

3. Pestaña **Configuración de transferencia**:
   - Límite de transferencias simultáneas: **2**
   - Timeout: **1000** segundos

4. **Conectar**

### Si falla con TLS

Probá en este orden (una configuración por intento):

1. `Requerir FTP explícito sobre TLS` ← el más común en Donweb
2. `Usar FTP explícito sobre TLS si está disponible`
3. `Solo FTP sin cifrado` (último recurso; menos seguro)

---

## Paso 3 — Errores frecuentes

| Mensaje en FileZilla | Causa | Solución |
|----------------------|-------|----------|
| `Connection refused` puerto 22 | Elegiste SFTP | Protocolo **FTP**, puerto **21** |
| `530 Login authentication failed` | Usuario o contraseña mal | Usuario **completo** con `@`; nueva contraseña en Ferozo |
| `Could not connect to server` | Host incorrecto o firewall | Usá host del panel, no el dominio propio |
| `425 Can't open data connection` | Modo pasivo/puertos | En Edición → Configuración → FTP: modo **Pasivo** |
| `Certificate` / TLS | Certificado del servidor | Aceptar certificado o probar otro modo de cifrado |
| Timeout | Muchas conexiones | Transferencias simultáneas = **2** |

---

## Paso 4 — Subir la app

1. En tu PC: publicá con Visual Studio → perfil **FolderProfile**  
   (genera la carpeta `publish/ferozo/`)

2. En FileZilla, panel derecho: entrá a **`public_html`**

3. Subí **el contenido** de `publish/ferozo/` (archivos sueltos), no la carpeta `ferozo` entera.

4. Verificá que existan `web.config`, `StockSantiCaza.Web.exe` (o dll) y `appsettings.Production.json`.

---

## Alternativa sin FileZilla

Ferozo → **Mi Sitio Web** → **Administrador de archivos** → subí un `.zip` y descomprimí dentro de `public_html`.

---

## Si nada funciona

1. Confirmá en Donweb que el plan es **Web Hosting Windows** (no Linux).
2. Escribí a soporte Donweb: “No puedo conectar por FTP puerto 21 FTPS a mi cuenta w400048”.
3. Pasame el **mensaje exacto** de la cola de FileZilla (pestaña inferior) para revisar el caso.
