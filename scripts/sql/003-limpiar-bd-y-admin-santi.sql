-- =============================================================================
-- StockSantiCAZA - Limpiar base de datos y crear administrador inicial
-- Base: w400048_santicazarmeria (Ferozo / DonWeb)
--
-- ATENCIÓN: BORRA TODOS LOS DATOS. Hacé backup antes de ejecutar.
--
-- Usuario creado:
--   Login:    Santi.F
--   Password: Santicaza
--   Rol:      Administrador (1)
-- =============================================================================

USE [w400048_santicazarmeria];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

PRINT 'Desactivando restricciones FK...';
EXEC sp_msforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';

PRINT 'Eliminando datos...';

IF OBJECT_ID(N'[dbo].[MovimientosProveedor]', N'U') IS NOT NULL
    DELETE FROM [dbo].[MovimientosProveedor];

IF OBJECT_ID(N'[dbo].[MovimientosStock]', N'U') IS NOT NULL
    DELETE FROM [dbo].[MovimientosStock];

IF OBJECT_ID(N'[dbo].[DetallesVenta]', N'U') IS NOT NULL
    DELETE FROM [dbo].[DetallesVenta];

IF OBJECT_ID(N'[dbo].[Ventas]', N'U') IS NOT NULL
    DELETE FROM [dbo].[Ventas];

IF OBJECT_ID(N'[dbo].[CredencialesCLU]', N'U') IS NOT NULL
    DELETE FROM [dbo].[CredencialesCLU];

IF OBJECT_ID(N'[dbo].[Armas]', N'U') IS NOT NULL
    DELETE FROM [dbo].[Armas];

IF OBJECT_ID(N'[dbo].[MunicionLotes]', N'U') IS NOT NULL
    DELETE FROM [dbo].[MunicionLotes];

IF OBJECT_ID(N'[dbo].[Proveedores]', N'U') IS NOT NULL
    DELETE FROM [dbo].[Proveedores];

IF OBJECT_ID(N'[dbo].[Clientes]', N'U') IS NOT NULL
    DELETE FROM [dbo].[Clientes];

IF OBJECT_ID(N'[dbo].[Productos]', N'U') IS NOT NULL
    DELETE FROM [dbo].[Productos];

IF OBJECT_ID(N'[dbo].[Usuarios]', N'U') IS NOT NULL
    DELETE FROM [dbo].[Usuarios];

PRINT 'Reactivando restricciones FK...';
EXEC sp_msforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';

PRINT 'Reiniciando contadores IDENTITY...';

IF OBJECT_ID(N'[dbo].[MovimientosProveedor]', N'U') IS NOT NULL
    DBCC CHECKIDENT ('[dbo].[MovimientosProveedor]', RESEED, 0);

IF OBJECT_ID(N'[dbo].[MovimientosStock]', N'U') IS NOT NULL
    DBCC CHECKIDENT ('[dbo].[MovimientosStock]', RESEED, 0);

IF OBJECT_ID(N'[dbo].[DetallesVenta]', N'U') IS NOT NULL
    DBCC CHECKIDENT ('[dbo].[DetallesVenta]', RESEED, 0);

IF OBJECT_ID(N'[dbo].[Ventas]', N'U') IS NOT NULL
    DBCC CHECKIDENT ('[dbo].[Ventas]', RESEED, 0);

IF OBJECT_ID(N'[dbo].[CredencialesCLU]', N'U') IS NOT NULL
    DBCC CHECKIDENT ('[dbo].[CredencialesCLU]', RESEED, 0);

IF OBJECT_ID(N'[dbo].[Armas]', N'U') IS NOT NULL
    DBCC CHECKIDENT ('[dbo].[Armas]', RESEED, 0);

IF OBJECT_ID(N'[dbo].[MunicionLotes]', N'U') IS NOT NULL
    DBCC CHECKIDENT ('[dbo].[MunicionLotes]', RESEED, 0);

IF OBJECT_ID(N'[dbo].[Proveedores]', N'U') IS NOT NULL
    DBCC CHECKIDENT ('[dbo].[Proveedores]', RESEED, 0);

IF OBJECT_ID(N'[dbo].[Clientes]', N'U') IS NOT NULL
    DBCC CHECKIDENT ('[dbo].[Clientes]', RESEED, 0);

IF OBJECT_ID(N'[dbo].[Productos]', N'U') IS NOT NULL
    DBCC CHECKIDENT ('[dbo].[Productos]', RESEED, 0);

IF OBJECT_ID(N'[dbo].[Usuarios]', N'U') IS NOT NULL
    DBCC CHECKIDENT ('[dbo].[Usuarios]', RESEED, 0);

PRINT 'Creando usuario administrador Santi.F...';

IF OBJECT_ID(N'[dbo].[Usuarios]', N'U') IS NOT NULL
BEGIN
    INSERT INTO [dbo].[Usuarios] ([Nombre], [Login], [PasswordHash], [Rol], [Activo])
    VALUES (
        N'Santi.F',
        N'Santi.F',
        N'AQAAAAIAAYagAAAAELzcRZvRcQtkkT8N0VphIsDxdACq29mJGb5J+cPPdfq+a6PsccTaeMOJcQY4AIad+w==',
        1,
        1
    );
END
ELSE
BEGIN
    RAISERROR('La tabla Usuarios no existe. Ejecutá primero la app o el script de esquema.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END

COMMIT TRANSACTION;

PRINT 'Listo. Base vacía con administrador Santi.F / Santicaza';

SELECT [Id], [Nombre], [Login], [Rol], [Activo]
FROM [dbo].[Usuarios];
GO
