# Alternativas si Ferozo no funciona

Si ves *"El servidor no respondió a tiempo"* en el login, el problema es uno de estos:

| Causa | Cómo detectarlo |
|-------|-----------------|
| Backend .NET apagado | `/api/health` no responde |
| Solo subiste HTML | `/api/health` no responde, pero ves el login |
| SQL mal configurada | `/api/health` OK, `/api/health/db` falla |
| Chrome bloquea el sitio | `/api/health` OK en otro navegador o celular |

---

## Error HTTP 500.31 — Failed to load ASP.NET Core runtime

**Causa:** IIS en Ferozo no tiene el *Hosting Bundle* de .NET 6, o el pool es incompatible con modo `inprocess`.

**Solución (ya en el repo):** `web.config` usa `hostingModel="outofprocess"` y el publish incluye `StockSantiCaza.Web.exe` autocontenido.

1. `git pull`
2. `.\scripts\publicar-donweb.ps1`
3. Subí **todo** de nuevo a `public_html` (sobrescribir `web.config` y `.exe`)
4. Reiniciá el sitio en el panel Ferozo
5. Probá `/api/health`

Si sigue con 500.31, probá publish **32 bits** (algunos pools de Ferozo son x86):

```powershell
dotnet publish src\StockSantiCaza.Web\StockSantiCaza.Web.csproj -c Release -r win-x86 --self-contained true -o "$env:USERPROFILE\Desktop\Publish"
```

---

## Opción 1 — Republicar autocontenido (RECOMENDADA)

Muchos hostings Ferozo **no tienen `dotnet` en el PATH**. La app nunca arranca.

**Solución:** publish **autocontenido** con `StockSantiCaza.Web.exe` (ya configurado en el repo).

```powershell
git pull
.\scripts\publicar-donweb.ps1
```

Subí **todo** `Desktop\Publish` a `public_html`. Debe incluir:

- `StockSantiCaza.Web.exe` ← importante
- `web.config` (apunta al `.exe`, no a `dotnet`)
- `appsettings.Production.json`
- Todas las `.dll`
- `wwwroot\`
- `logs\`

Probá: `https://TU-DOMINIO/api/health`

---

## Opción 2 — Usar solo en tu PC (sin DonWeb)

Para operar localmente mientras resolvés el hosting:

1. SQL Server Express en tu notebook (`appsettings.Development.json`)
2. F5 en Visual Studio
3. Abrís `https://localhost:.../login`

La base local es independiente de DonWeb.

---

## Opción 3 — Hosting .NET en la nube + SQL de DonWeb

**Limitación:** la base `sql2016` de DonWeb **solo funciona desde el mismo hosting DonWeb**, no desde Azure/Railway externos.

Si querés API en Azure/Railway, necesitás **migrar la base** a:

- Azure SQL Database, o
- SQL Server en un VPS

Es un cambio de infraestructura, no solo de código.

---

## Opción 4 — Cambiar plan en DonWeb

Verificá en el panel que tengas:

- Hosting **Windows** (no Linux)
- Soporte **ASP.NET Core / .NET 6**
- Módulo **ASP.NET Core Hosting Bundle** instalado

Si tenés plan Linux compartido, la app .NET **no va a funcionar**. Habría que contratar Windows Hosting o usar Opción 3.

---

## Opción 5 — Dominio bloqueado por Chrome

Si `/api/health` responde desde el celular pero Chrome dice "Sitio peligroso":

1. Limpiá archivos `.php` o basura en `public_html`
2. Activá SSL en DonWeb
3. Pedí revisión en [Google Search Console](https://search.google.com/search-console)

---

## Qué NO funciona

| Idea | Por qué no |
|------|------------|
| Subir solo `login.html` | Sin `.exe`/`.dll` no hay API |
| Solo API sin HTML | Necesitás ambos en el mismo sitio |
| Base `sql2016` desde tu PC sin túnel | Solo funciona dentro de DonWeb |
| PHP en el mismo hosting | Habría que reescribir todo el sistema |

---

## Orden de prueba después de republicar

```text
1. /api/health
2. /api/health/db
3. /login  (el diagnóstico automático te dice qué falla)
4. Ingresar con tu usuario
```

Si el paso 1 falla → problema de hosting/IIS, no de usuario/contraseña.

Ver también: [DIAGNOSTICO-HOSTING.md](./DIAGNOSTICO-HOSTING.md), [PUBLICAR-SIMPLE.md](./PUBLICAR-SIMPLE.md)
