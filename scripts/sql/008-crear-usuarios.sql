-- =============================================================================
-- StockSantiCAZA - Crear usuarios (sin borrar datos existentes)
-- Base: w400048_santicazarmeria (Ferozo / DonWeb)
--
-- RolUsuario:
--   1 = Administrador
--   2 = Vendedor
--
-- Contraseñas iniciales de los ejemplos:
--   Santi.F      -> Santicaza
--   Mati.F       -> Santicaza
--   admin        -> Admin123!
--
-- Si el login ya existe, el INSERT se omite.
-- Para cambiar contraseña después, usá la pantalla Usuarios de la app
-- o generá un nuevo PasswordHash con ASP.NET Identity (ver nota al final).
-- =============================================================================

USE [w400048_santicazarmeria];
GO

SET NOCOUNT ON;

PRINT 'Creando usuarios si no existen...';

-- Administrador principal (mismo que script 003)
IF OBJECT_ID(N'[dbo].[Usuarios]', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM [dbo].[Usuarios] WHERE [Login] = N'Santi.F')
BEGIN
    INSERT INTO [dbo].[Usuarios] ([Nombre], [Login], [PasswordHash], [Rol], [Activo])
    VALUES (
        N'Santiago Ferreyra',
        N'Santi.F',
        N'AQAAAAIAAYagAAAAELzcRZvRcQtkkT8N0VphIsDxdACq29mJGb5J+cPPdfq+a6PsccTaeMOJcQY4AIad+w==',
        1,
        1
    );
    PRINT 'Usuario creado: Santi.F (Administrador)';
END
ELSE
    PRINT 'Usuario Santi.F ya existe, se omite.';

-- Vendedor: Matias Ferreyra
IF OBJECT_ID(N'[dbo].[Usuarios]', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM [dbo].[Usuarios] WHERE [Login] = N'Mati.F')
BEGIN
    INSERT INTO [dbo].[Usuarios] ([Nombre], [Login], [PasswordHash], [Rol], [Activo])
    VALUES (
        N'Matias Ferreyra',
        N'Mati.F',
        N'AQAAAAIAAYagAAAAECwVvYhiipBfqtwBTzhg/OTxvpgoEtJDnSJyN6C4ph6iL7YyNQ6lTh2kVVnStG+SEQ==',
        2,
        1
    );
    PRINT 'Usuario creado: Mati.F (Vendedor)';
END
ELSE
    PRINT 'Usuario Mati.F ya existe, se omite.';

-- Administrador alternativo (admin / Admin123!)
IF OBJECT_ID(N'[dbo].[Usuarios]', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM [dbo].[Usuarios] WHERE [Login] = N'admin')
BEGIN
    INSERT INTO [dbo].[Usuarios] ([Nombre], [Login], [PasswordHash], [Rol], [Activo])
    VALUES (
        N'Administrador',
        N'admin',
        N'AQAAAAIAAYagAAAAEIlOFkpzMRbSNIO236urRL07xKd5K4wbpCdbcmvfPKSe+xC/0iji6o9Up+QgHl+QaA==',
        1,
        1
    );
    PRINT 'Usuario creado: admin (Administrador)';
END
ELSE
    PRINT 'Usuario admin ya existe, se omite.';

PRINT '';
PRINT 'Usuarios actuales:';

SELECT [Id], [Nombre], [Login],
       CASE [Rol]
           WHEN 1 THEN N'Administrador'
           WHEN 2 THEN N'Vendedor'
           ELSE CAST([Rol] AS nvarchar(20))
       END AS [Rol],
       [Activo]
FROM [dbo].[Usuarios]
ORDER BY [Id];
GO

-- =============================================================================
-- PLANTILLA: agregar otro usuario
-- Copiá este bloque, cambiá los valores y reemplazá PasswordHash si hace falta.
--
-- IF NOT EXISTS (SELECT 1 FROM [dbo].[Usuarios] WHERE [Login] = N'TU_LOGIN')
-- BEGIN
--     INSERT INTO [dbo].[Usuarios] ([Nombre], [Login], [PasswordHash], [Rol], [Activo])
--     VALUES (
--         N'Nombre completo',
--         N'TU_LOGIN',
--         N'AQAAAAIAAYagAAAAE...hash...',
--         2,   -- 1=Administrador, 2=Vendedor
--         1
--     );
-- END
-- =============================================================================
