# Ferozo: la API responde pero la base no conecta

Síntomas:

- `/api/health` → `ok`, `sqlServer: sql2016`, `tieneProductionJson: true`
- `/api/health/db` → `unreachable` o `error`
- Login → “La base de datos no respondió”

La configuración **está leída**, pero el servidor web **no logra abrir SQL**.

---

## Paso 1 — Cadena en `web.config` (más confiable que JSON)

Editá por FTP `public_html/web.config` y agregá la cadena como variable de entorno (reemplazá la contraseña):

```xml
<environmentVariables>
  <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
  <environmentVariable name="ConnectionStrings__DefaultConnection" value="Server=sql2016;Database=w400048_santicazarmeria;User Id=w400048_MariAdmin;Password=TU_PASSWORD;Integrated Security=False;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=true;Connection Timeout=60" />
</environmentVariables>
```

**Importante:** `Integrated Security=False` fuerza usuario/contraseña SQL.

Guardá, esperá 1 minuto, probá `/api/health/db`.

---

## Paso 2 — Verificar plan de hosting

DonWeb tiene planes **Linux** y **Windows**.

| Plan | SQL Server `sql2016` | Integrated Security |
|------|----------------------|---------------------|
| **Windows Hosting** | Sí (misma cuenta) | Puede funcionar |
| **Linux Hosting** | A veces no alcanza | No funciona |

ASP.NET Core + SQL Server 2016 en Ferozo requiere normalmente **Windows Hosting**.

En el panel Ferozo → tu sitio → debe decir **Windows** / **ASP.NET**, no solo PHP/Linux.

Si el sitio está en Linux, contactá a DonWeb para migrar a Windows o confirmar si `sql2016` es accesible desde ese servidor.

---

## Paso 3 — Revisar logs en el servidor

En FTP, carpeta `public_html/logs/`, archivo `stdout_*.log`.

Buscá líneas como:

- `Login failed for user 'w400048_MariAdmin'`
- `A network-related or instance-specific error`
- `Integrated Security`

Eso indica si es contraseña, red o tipo de autenticación.

---

## Paso 4 — Variables duplicadas en el panel

En Ferozo, si existe **ConnectionStrings__DefaultConnection** en variables del panel con un valor viejo, **pisa** el `appsettings.Production.json`.

Borrá esa variable o actualizala con la cadena correcta.

---

## Paso 5 — Ticket a soporte DonWeb

Si nada conecta, abrí ticket con este texto:

> Tengo una aplicación ASP.NET Core 6 publicada en `public_html` de la cuenta `w400048`.
> La API responde en `https://santicazastock.com.ar/api/health` pero no conecta a SQL Server.
> Base: `w400048_santicazarmeria`, servidor: `sql2016`, usuario: `w400048_MariAdmin`.
> Desde el administrador SQL del panel puedo ejecutar queries, pero la app no conecta.
> ¿Cuál es la cadena de conexión correcta para ASP.NET Core en mi plan?
> ¿El sitio debe estar en Windows Hosting?

---

## Cuando `/api/health/db` diga `connected`

Login con el usuario creado en SQL:

- Usuario: `Santi.F`
- Contraseña: `Santicaza`
