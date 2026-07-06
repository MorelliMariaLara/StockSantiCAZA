-- =============================================================================
-- StockSantiCAZA - Corregir usuarios y contraseñas (solo base de datos)
-- Base: w400048_santicazarmeria
--
-- El login NO depende del rol: solo valida Login + Activo + PasswordHash.
-- Los vendedores fallaban porque el PasswordHash estaba mal generado.
--
-- Contraseñas después de ejecutar este script:
--   Santi.F  -> Santicaza
--   Mati.F   -> Maticaza
--   Gonzi.F  -> Gonzicaza
--   Eze.F    -> Ezecaza
--
-- Hashes generados con ASP.NET Core 6 PasswordHasher (mismo que usa la app).
-- =============================================================================

USE [w400048_santicazarmeria];
GO

SET NOCOUNT ON;

PRINT 'Corrigiendo usuarios...';

-- Asegurar Activo = 1 y limpiar espacios en Login/PasswordHash
UPDATE [dbo].[Usuarios]
SET
    [Login] = RTRIM(LTRIM([Login])),
    [Nombre] = RTRIM(LTRIM([Nombre])),
    [PasswordHash] = RTRIM(LTRIM([PasswordHash])),
    [Activo] = 1
WHERE [Login] IN ('Santi.F', 'Mati.F', 'Gonzi.F', 'Eze.F');

-- Administrador
UPDATE [dbo].[Usuarios]
SET
    [Nombre] = 'Santi.F',
    [PasswordHash] = 'AQAAAAEAACcQAAAAEMPMJ27FoY+fO2atkkqfhwT4RPo9/NhbBYXgdmc+6TPyyU/4efctOW9hy78+f9wDzg==',
    [Rol] = 1,
    [Activo] = 1
WHERE [Login] = 'Santi.F';

-- Vendedores
UPDATE [dbo].[Usuarios]
SET
    [Nombre] = 'Matias Ferreyra',
    [PasswordHash] = 'AQAAAAEAACcQAAAAEAuc2GigwqJevBozhTNSvTEtSYqHMNpHshW5qUiJ82pmXNJiR1i054UloxgJIbWlMw==',
    [Rol] = 2,
    [Activo] = 1
WHERE [Login] = 'Mati.F';

UPDATE [dbo].[Usuarios]
SET
    [Nombre] = 'Gonzalo',
    [PasswordHash] = 'AQAAAAEAACcQAAAAEFQIFsYs3cLXtrC6QSf4Jn5gVGc0a/PIDd0GizSO6nMopepGjev0zQr6DCaZeghbwA==',
    [Rol] = 2,
    [Activo] = 1
WHERE [Login] = 'Gonzi.F';

UPDATE [dbo].[Usuarios]
SET
    [Nombre] = 'Ezequiel',
    [PasswordHash] = 'AQAAAAEAACcQAAAAEMpp+FqYPwQmeLbuth7tria8YIbgbs6544VjJdbanEOWeOHTJelvwayfshlRejUApw==',
    [Rol] = 2,
    [Activo] = 1
WHERE [Login] = 'Eze.F';

PRINT '';
PRINT 'Usuarios corregidos:';

SELECT
    [Id],
    [Nombre],
    [Login],
    [Rol],
    CASE [Rol]
        WHEN 1 THEN 'Administrador'
        WHEN 2 THEN 'Vendedor'
        ELSE CAST([Rol] AS varchar(20))
    END AS [RolNombre],
    [Activo],
    LEN([PasswordHash]) AS [HashLength]
FROM [dbo].[Usuarios]
ORDER BY [Id];
GO
