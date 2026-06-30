# Publicar en Ferozo — guía rápida

## Si Visual Studio dice "Error de compilación" pero el log muestra FTP timeout

**La compilación ya funcionó.** El fallo es al **subir por FTP**, no al compilar.

Ejemplo del error real:
```text
Conectándose a ftp://w400048.ferozo.com/public_html...
The server connection timed out.
```

---

## Método recomendado (más confiable)

### 1. Publicar en carpeta local

En Visual Studio:
1. Clic derecho en **StockSantiCaza.Web** → **Publicar**
2. Elegir perfil **FolderProfile** (carpeta `publish` dentro del proyecto)
3. Publicar

O por consola:
```bash
cd src\StockSantiCaza.Web
dotnet publish -c Release -o publish
```

### 2. Subir con FileZilla (o el administrador de archivos de Ferozo)

- Conectate al FTP con los datos del panel DonWeb/Ferozo
- Ruta destino habitual: `public_html` del dominio  
  (a veces: `stock.santicazaarmeria.com.ar/public_html`)
- Subí **todo el contenido** de la carpeta `publish\` (no la carpeta `publish` en sí)

---

## Si querés publicar directo desde Visual Studio por FTP

En el perfil FTP verificá:

| Campo | Valor típico |
|-------|----------------|
| Servidor | `ftp://w400048.ferozo.com` o el que indique el panel |
| Ruta del sitio | `stock.santicazaarmeria.com.ar/public_html` |
| Modo pasivo | Activado |
| Usuario | `w400048@w400048.ferozo.com` |

### Si sigue con timeout

1. Probá subir con **FileZilla** — si también falla, es red/firewall o datos FTP incorrectos
2. Revisá en el panel Ferozo que el usuario FTP esté activo
3. Desactivá temporalmente antivirus/firewall
4. Probá otra red (datos del celular)
5. Contactá soporte DonWeb si el FTP no responde

---

## Después de subir

1. En Ferozo, variable `ASPNETCORE_ENVIRONMENT` = `Production`
2. Cadena SQL en `appsettings.Production.json` o variable de entorno
3. Creá la carpeta `logs` en `public_html` si no se subió con el publish
4. Probá diagnóstico: `/api/health` → `/api/health/db` → login
5. Guía completa: [DIAGNOSTICO-HOSTING.md](./DIAGNOSTICO-HOSTING.md)
