# Conexión SQL en Ferozo — respuesta oficial DonWeb

## Cadena correcta (confirmada por soporte)

**No uses IP ni puerto** (ej. `200.58.120.140:2082` no es SQL Server).

**DonWeb dice `Server=sql2016`.** En ASP.NET Core 6 además hay que forzar **TCP puerto 1433** (no Named Pipes):

```text
Server=sql2016,1433;Database=w400048_santicazarmeria;User Id=w400048_MariAdmin;...
```

Esto **no** es una IP (`200.58.120.140:2082` estaba mal). Sigue siendo el servidor `sql2016` con el puerto estándar de SQL Server.

En `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sql2016,1433;Database=w400048_santicazarmeria;User Id=w400048_MariAdmin;Integrated Security=False;TrustServerCertificate=True;Encrypt=False;Connection Timeout=30"
  },
  "Database": {
    "SkipInitialization": true,
    "SqlPassword": "TU_NUEVA_CONTRASEÑA_DEL_PANEL"
  }
}
```

La contraseña va en `SqlPassword` (no en la cadena) si tiene caracteres como `@`.

---

## Cambiar contraseña SQL (recomendado por DonWeb)

1. Panel Ferozo → **Bases de datos** → SQL Server → usuario `w400048_MariAdmin`
2. Generá una **contraseña nueva**
3. Actualizá `Database.SqlPassword` en `appsettings.Production.json`
4. Republicá y subí por FileZilla

---

## Probar conexión

```
https://santicazastock.com.ar/api/health/sql-probe?metodo=1
```

- `metodo=1` → `sql2016` + usuario SQL (oficial DonWeb)
- `metodo=2` → `sql2016` + Integrated Security (cadena del panel)

---

## Límites del plan (hosting compartido Windows)

| Límite | Valor |
|--------|-------|
| Memoria privada App Pool | 154 MB |
| Memoria virtual | 1.8 GB |
| Reciclado automático | cada 180 min |

Si la app supera 154 MB, el App Pool se recicla → errores 502 o login que no responde.

**No se puede** cambiar Application Pool, IIS ni puertos en este plan.

Si necesitás más recursos → **Cloud Server** en DonWeb.

---

## Publicar

```powershell
git pull origin main
.\scripts\publicar-donweb.ps1
```

Subir todo `publish/` a `public_html` por FileZilla.

Verificar:
- `/login` → formulario visible
- `/api/health/db` → `"connected"` o error SQL claro
- Login app: `Santi.F` / `Santicaza`
