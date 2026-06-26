# Migración StockSantiCAZA → MySQL

## Archivos

| Archivo | Uso |
|---------|-----|
| `000-esquema-completo.sql` | Crea la base y las 11 tablas con índices y FKs |
| `001-migrar-datos.sql` | Guía y pasos post-migración de datos |

## Paso 1: Crear esquema en MySQL

```bash
mysql -u root -p < scripts/mysql/000-esquema-completo.sql
```

O desde **MySQL Workbench** / **DBeaver**: abrir y ejecutar `000-esquema-completo.sql`.

## Paso 2: Migrar datos desde SQL Server

### Herramientas recomendadas

1. **MySQL Workbench** → Migration Wizard (SQL Server → MySQL)
2. **SSMS** → Export Data → destino MySQL ODBC
3. **pgloader** no aplica; para SQL Server→MySQL usar **AWS DMS**, **Full Convert**, o export CSV + `LOAD DATA INFILE`

### Orden obligatorio de tablas

1. Usuarios  
2. Productos  
3. Clientes  
4. Proveedores  
5. CredencialesCLU  
6. Armas  
7. MunicionLotes  
8. Ventas  
9. DetallesVenta  
10. MovimientosStock  
11. MovimientosProveedor  

### Conversión de tipos

- `bit` → `TINYINT(1)` (0/1)
- `nvarchar` → `VARCHAR` con `utf8mb4`
- `datetime2` → `DATETIME(6)`
- Enums de la app → mismos enteros en columna `INT`

## Paso 3: Conectar la aplicación .NET a MySQL

1. Instalar paquete NuGet:

```bash
dotnet add package Pomelo.EntityFrameworkCore.MySql
```

2. En `Program.cs`, reemplazar SQL Server por MySQL:

```csharp
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)));
```

3. En `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=3306;Database=StockSantiCaza;User=root;Password=TU_PASSWORD;"
}
```

4. Quitar o adaptar `EnsureProveedoresSchemaAsync` en `DbInitializer` (el script SQL ya crea proveedores).

## Tablas del esquema

- `Usuarios`
- `Productos`
- `Clientes`
- `Proveedores`
- `CredencialesCLU`
- `Armas`
- `MunicionLotes`
- `Ventas`
- `DetallesVenta`
- `MovimientosStock`
- `MovimientosProveedor`

## Referencia de enums (valores INT)

Ver comentarios en `000-esquema-completo.sql` o `Models/Enums.cs` en el código fuente.
