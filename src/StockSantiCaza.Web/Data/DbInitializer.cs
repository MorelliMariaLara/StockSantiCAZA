using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockSantiCaza.Web.Models;

namespace StockSantiCaza.Web.Data;

public static class DbInitializer
{
    public static async Task ApplyMigrationsAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        var applied = (await db.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();

        logger.LogInformation(
            "Migraciones EF: {Applied} aplicadas, {Pending} pendientes",
            applied.Count,
            pending.Count);

        if (pending.Count > 0)
        {
            logger.LogWarning(
                "Aplicando migraciones pendientes en StockSantiCAZA: {Migrations}",
                string.Join(", ", pending));

            await db.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Migraciones aplicadas correctamente.");
            return;
        }

        if (!await SchemaExistsAsync(db, cancellationToken))
        {
            logger.LogWarning(
                "La base StockSantiCAZA no contiene las tablas de la aplicación. Ejecutando MigrateAsync.");

            await db.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Esquema creado correctamente.");
        }
    }

    public static async Task SeedDemoDataAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (await db.Productos.AnyAsync(cancellationToken))
        {
            logger.LogInformation("La base ya contiene datos. No se cargará el seed demo.");
            return;
        }

        logger.LogInformation("Cargando datos demo de prueba...");

        var clienteVigente = new Cliente
        {
            NombreRazonSocial = "Juan Pérez",
            DniCuit = "30123456",
            Email = "juan.perez@example.com",
            Telefono = "11-5555-1234",
            Domicilio = "Av. Rivadavia 1200, CABA",
            CredencialCLU = new CredencialCLU
            {
                NumeroLegajo = "CLU-2024-00123",
                FechaEmision = DateOnly.FromDateTime(DateTime.Today.AddYears(-1)),
                FechaVencimiento = DateOnly.FromDateTime(DateTime.Today.AddMonths(6))
            }
        };

        var clienteVencido = new Cliente
        {
            NombreRazonSocial = "María Gómez",
            DniCuit = "27987654",
            Email = "maria.gomez@example.com",
            Telefono = "11-4444-5678",
            CredencialCLU = new CredencialCLU
            {
                NumeroLegajo = "CLU-2022-00999",
                FechaEmision = DateOnly.FromDateTime(DateTime.Today.AddYears(-3)),
                FechaVencimiento = DateOnly.FromDateTime(DateTime.Today.AddMonths(-2))
            }
        };

        var productoGeneral = new Producto
        {
            Sku = "ACC-CHALECO-01",
            Nombre = "Chaleco táctico",
            Descripcion = "Chaleco multipropósito talla L",
            Categoria = ProductoCategoria.General,
            Marca = "TacGear",
            PrecioUnitario = 85000m,
            AlicuotaIva = 21m,
            StockActual = 12,
            StockMinimo = 3
        };

        var productoArma = new Producto
        {
            Sku = "ARM-BERSA-T9",
            Nombre = "Pistola Bersa Thunder 9",
            Categoria = ProductoCategoria.Arma,
            Marca = "Bersa",
            Modelo = "Thunder 9",
            Calibre = "9mm",
            PrecioUnitario = 650000m,
            AlicuotaIva = 21m,
            StockActual = 2,
            StockMinimo = 1,
            Armas =
            [
                new Arma
                {
                    NumeroSerie = "BST9-2025-00045",
                    Marca = "Bersa",
                    Modelo = "Thunder 9",
                    Calibre = "9mm",
                    TipoArma = TipoArma.Pistola,
                    EstadoTramiteAnmac = EstadoTramiteAnmac.Autorizado
                },
                new Arma
                {
                    NumeroSerie = "BST9-2025-00046",
                    Marca = "Bersa",
                    Modelo = "Thunder 9",
                    Calibre = "9mm",
                    TipoArma = TipoArma.Pistola,
                    EstadoTramiteAnmac = EstadoTramiteAnmac.Autorizado
                }
            ]
        };

        var productoMunicion = new Producto
        {
            Sku = "MUN-9MM-FMJ",
            Nombre = "Munición 9mm FMJ",
            Categoria = ProductoCategoria.Municion,
            Calibre = "9mm",
            PrecioUnitario = 3500m,
            AlicuotaIva = 21m,
            StockActual = 500,
            StockMinimo = 100,
            LotesMunicion =
            [
                new MunicionLote
                {
                    NumeroLote = "LOT-9MM-2025-A",
                    Calibre = "9mm",
                    TipoMunicion = TipoMunicion.Cartucho,
                    CantidadDisponible = 300
                },
                new MunicionLote
                {
                    NumeroLote = "LOT-9MM-2025-B",
                    Calibre = "9mm",
                    TipoMunicion = TipoMunicion.Cartucho,
                    CantidadDisponible = 200
                }
            ]
        };

        var productoStockBajo = new Producto
        {
            Sku = "OPT-MIRA-01",
            Nombre = "Mira réflex compacta",
            Categoria = ProductoCategoria.General,
            Marca = "OptiPro",
            PrecioUnitario = 120000m,
            AlicuotaIva = 21m,
            StockActual = 1,
            StockMinimo = 5
        };

        db.Clientes.AddRange(clienteVigente, clienteVencido);
        db.Productos.AddRange(productoGeneral, productoArma, productoMunicion, productoStockBajo);
        await db.SaveChangesAsync(cancellationToken);

        db.Armas.Add(new Arma
        {
            ProductoId = productoArma.Id,
            ClienteActualId = clienteVigente.Id,
            NumeroSerie = "REG-9MM-001",
            Marca = "Bersa",
            Modelo = "Thunder 9",
            Calibre = "9mm",
            TipoArma = TipoArma.Pistola,
            EstadoTramiteAnmac = EstadoTramiteAnmac.Entregado,
            FechaTransferencia = DateTime.UtcNow.AddYears(-1)
        });
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Datos demo cargados correctamente.");
    }

    public static async Task InitializeAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await ApplyMigrationsAsync(db, logger, cancellationToken);
        await SeedDemoDataAsync(db, logger, cancellationToken);
    }

    private static async Task<bool> SchemaExistsAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        try
        {
            await db.Ventas.AnyAsync(cancellationToken);
            return true;
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 208)
        {
            return false;
        }
    }
}
