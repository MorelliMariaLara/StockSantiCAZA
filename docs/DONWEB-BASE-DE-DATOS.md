# Base de datos en DonWeb / Ferozo

## Qué hace la aplicación al iniciar

La inicialización corre **en segundo plano** para que la página no se cuelgue en DonWeb.

Al arrancar, la app revisa la base configurada en `appsettings.Production.json`:

1. Si **no existe la tabla `Usuarios`** → crea todas las tablas (`EnsureCreated`).
2. Si la base **ya existía** → aplica migraciones idempotentes (script legacy).
3. Si **no hay usuarios** → crea `admin` / `Admin123!`.

En el login verás *"La base de datos se está inicializando..."* la primera vez. Esperá hasta que desaparezca el mensaje.

---

## Pasos en DonWeb (base nueva o vacía)

### 1. Verificar la base en el panel

- Base: `w400048_santicazaarmeria`
- Usuario SQL: `w400048_MariAdmin`
- Servidor (desde el hosting): `sql2016`

### 2. Configurar `appsettings.Production.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sql2016;Database=w400048_santicazaarmeria;User Id=w400048_MariAdmin;Password=TU_PASSWORD;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False;Connection Timeout=60"
  },
  "Database": {
    "SkipInitialization": false
  }
}
```

`SkipInitialization` debe estar en **`false`** (o no existir la clave).

### 3. Publicar y subir de nuevo

```bash
dotnet publish -c Release -o publish
```

Subí todo el publish a `public_html`, incluido `appsettings.Production.json`.

### 4. Reiniciar el sitio

En el panel Ferozo, reiniciá la aplicación o el pool de IIS.

### 5. Probar login

- Usuario: `admin`
- Contraseña: `Admin123!`

---

## Si la base ya tenía datos viejos

La app **no borra datos**. Solo:

- Crea tablas que falten.
- Aplica cambios de columnas/índices del script de migración.
- Crea el usuario `admin` solo si la tabla `Usuarios` está vacía.

Si tenés un esquema muy distinto o corrupto, usá los scripts en `scripts/sql/` con cuidado (algunos borran datos).

---

## Desactivar la inicialización automática

Solo si ya tenés todo el esquema y no querés que la app toque la BD al iniciar:

```json
"Database": {
  "SkipInitialization": true
}
```

Si la base está vacía y ponés `true`, el login fallará porque no habrá tablas.

---

## Diagnóstico si falla

1. Activá logs en `web.config`: `stdoutLogEnabled="true"`.
2. Buscá en `logs/stdout_*.log` líneas `[DbInitializer]`.
3. Errores frecuentes:
   - Contraseña SQL incorrecta.
   - Base `w400048_santicazaarmeria` no creada en el panel.
   - Usuario SQL sin permiso `db_owner` o `ddladmin`.

---

## Alternativa manual (sin depender del arranque de la app)

Si no podés hacer que la app arranque pero sí ejecutar SQL en el panel:

1. La base vacía necesita primero el esquema completo. Eso lo genera `EnsureCreated` al iniciar la app una vez.
2. Para bases que **ya tienen** tablas antiguas, ejecutá en SSMS o el administrador SQL de DonWeb:
   - `scripts/sql/007-migracion-completa.sql`

Ese script **no crea** tablas base desde cero; asume que `Productos`, `Clientes`, etc. ya existen.
