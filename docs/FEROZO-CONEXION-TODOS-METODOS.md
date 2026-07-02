# Ferozo: todos los métodos para conectar SQL

Guía profesional para la cuenta **w400048**, base **w400048_santicazarmeria**.

---

## Paso 1 — Diagnosticar (sin romper el login)

Republicá la app y abrí en el navegador:

```
https://santicazastock.com.ar/api/health/sql-probe
```

La app **prueba automáticamente** estos métodos:

| # | Método | Cuándo sirve |
|---|--------|--------------|
| 1 | `sql2016,1433` + usuario SQL | **Más común** en ASP.NET Core |
| 2 | `sql2016` + usuario SQL | Red interna Ferozo |
| 3 | `tcp:sql2016,1433` + usuario SQL | Si el panel pide TCP explícito |
| 4 | `sql2016,1433` + Integrated Security | Panel SSPI, algunos planes Windows |
| 5 | `sql2016` + Integrated Security | Cadena del panel DonWeb |

Si alguno dice `"ok": true`, copiá el `dataSourceGanador` a `appsettings.Production.json`:

```json
"Database": {
  "SkipInitialization": true,
  "SqlPassword": "SantiagoFerreyra@22",
  "DataSource": "sql2016,1433"
}
```

Republicá y probá login.

---

## Paso 2 — Configuración en `appsettings.Production.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sql2016;Database=w400048_santicazarmeria;User Id=w400048_MariAdmin;Integrated Security=False;TrustServerCertificate=True;Encrypt=False;Connection Timeout=30"
  },
  "Database": {
    "SkipInitialization": true,
    "SqlPassword": "SantiagoFerreyra@22"
  }
}
```

- Contraseña con `@` → siempre en `SqlPassword`, nunca en la cadena.
- Si `sql-probe` encontró un `DataSource`, agregalo en `Database`.

---

## Paso 3 — Cadena en `web.config` (alternativa)

Si el JSON no alcanza, editá `public_html/web.config` por FTP:

```xml
<environmentVariable name="ConnectionStrings__DefaultConnection"
  value="Server=sql2016,1433;Database=w400048_santicazarmeria;User Id=w400048_MariAdmin;Password=SantiagoFerreyra@22;Integrated Security=False;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30" />
```

**Cuidado:** si el panel Ferozo también tiene `ConnectionStrings__DefaultConnection`, una pisa a la otra.

---

## Paso 4 — Panel Ferozo

| Qué revisar | Dónde |
|-------------|-------|
| Plan **Windows** + .NET 6 | Configuración del sitio |
| Base SQL existe | Bases de datos → SQL Server |
| Usuario `w400048_MariAdmin` | Misma sección |
| Variables de entorno viejas | Borrar `ConnectionStrings__DefaultConnection` si está mal |

---

## Paso 5 — Errores frecuentes

| Error | Significado | Acción |
|-------|-------------|--------|
| **53** | Red: no encuentra el servidor | Probar otro `DataSource` con sql-probe |
| **11001** | DNS: no resuelve el host | No usar `tcp:` si falla; probar `sql2016,1433` |
| **18456** | Login failed | Contraseña SQL incorrecta en panel |
| **502.3** | Worker caído | No tocar arranque; republicar .dll completo |
| Login vacío | Error sin mensaje | Ya corregido: ahora muestra detalle SQL |

---

## Paso 6 — Ticket DonWeb (si sql-probe falla todo)

> Cuenta w400048, sitio santicazastock.com.ar, ASP.NET Core 6 en Windows Hosting.  
> Base w400048_santicazarmeria en sql2016, usuario w400048_MariAdmin.  
> Desde el panel SQL ejecuto queries bien, pero la app en public_html no conecta (error 53/11001).  
> ¿Cuál es el **Data Source** exacto para ASP.NET Core? ¿Hay IP interna?  
> ¿Mi plan web tiene acceso de red al servidor sql2016?

---

## Paso 7 — Usuarios de la app (cuando SQL conecte)

| Usuario | Contraseña |
|---------|------------|
| `Santi.F` | `Santicaza` |
| `admin` | `Admin123!` |

---

## Publicar

```powershell
git pull origin main
.\scripts\publicar-donweb.ps1
```

Subir **todo** `publish/` a `public_html` por FileZilla.
