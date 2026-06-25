using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Data;
using StockSantiCaza.Web.Models;
using StockSantiCaza.Web.Services.Facturacion;

namespace StockSantiCaza.Web.Services.Ventas;

public class VentasService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IFacturacionElectronicaService facturacionElectronicaService) : IVentasService
{
    public async Task<VentaConfirmadaDto> ConfirmarVentaAsync(
        NuevaVentaRequest request,
        CancellationToken cancellationToken = default)
    {
        var errores = new List<string>();
        if (request.ClienteId is null)
        {
            errores.Add("Debe seleccionar un cliente.");
        }

        if (request.Items.Count == 0)
        {
            errores.Add("Debe agregar al menos un producto a la venta.");
        }

        if (errores.Count > 0)
        {
            throw new VentaValidationException(errores);
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var cliente = await db.Clientes
            .Include(x => x.CredencialCLU)
            .Include(x => x.ArmasRegistradas)
            .SingleOrDefaultAsync(x => x.Id == request.ClienteId, cancellationToken);

        if (cliente is null)
        {
            throw new VentaValidationException(["El cliente seleccionado no existe."]);
        }

        var productoIds = request.Items.Select(x => x.ProductoId).Distinct().ToArray();
        var productos = await db.Productos
            .Where(x => productoIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var armaIds = request.Items.Where(x => x.ArmaId.HasValue).Select(x => x.ArmaId!.Value).Distinct().ToArray();
        var armas = await db.Armas
            .Include(x => x.Producto)
            .Where(x => armaIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var loteIds = request.Items.Where(x => x.MunicionLoteId.HasValue).Select(x => x.MunicionLoteId!.Value).Distinct().ToArray();
        var lotes = await db.MunicionLotes
            .Include(x => x.Producto)
            .Where(x => loteIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var contieneControlado = request.Items.Any(item =>
            productos.TryGetValue(item.ProductoId, out var producto)
            && producto.Categoria is ProductoCategoria.Arma or ProductoCategoria.Municion);

        ValidarCredencial(cliente, contieneControlado, errores);

        var venta = new Venta
        {
            ClienteId = cliente.Id,
            TipoComprobante = request.TipoComprobante,
            Estado = request.TipoComprobante == TipoComprobante.Presupuesto ? EstadoVenta.Confirmada : EstadoVenta.Facturada,
            DescuentoTotal = request.DescuentoGeneral,
            Observaciones = request.Observaciones,
            Fecha = DateTime.UtcNow
        };

        var descuentaStock = request.TipoComprobante != TipoComprobante.Presupuesto;
        var calibresHabilitados = cliente.ArmasRegistradas
            .Select(x => NormalizarCalibre(x.Calibre))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var arma in armas.Values)
        {
            calibresHabilitados.Add(NormalizarCalibre(arma.Calibre));
        }

        foreach (var item in request.Items)
        {
            if (!productos.TryGetValue(item.ProductoId, out var producto))
            {
                errores.Add($"El producto con ID {item.ProductoId} no existe.");
                continue;
            }

            ValidarItemBasico(item, producto, errores);

            Arma? arma = null;
            MunicionLote? lote = null;

            if (producto.Categoria == ProductoCategoria.Arma)
            {
                arma = ValidarArma(item, producto, armas, errores);
                if (arma is not null)
                {
                    calibresHabilitados.Add(NormalizarCalibre(arma.Calibre));
                }
            }
            else if (producto.Categoria == ProductoCategoria.Municion)
            {
                lote = ValidarMunicion(item, producto, lotes, calibresHabilitados, errores);
            }

            var lineaBruta = item.PrecioUnitario * item.Cantidad;
            var descuento = Math.Min(item.Descuento, lineaBruta);
            var baseImponible = lineaBruta - descuento;
            var iva = Math.Round(baseImponible * producto.AlicuotaIva / 100m, 2, MidpointRounding.AwayFromZero);

            venta.Detalles.Add(new DetalleVenta
            {
                ProductoId = producto.Id,
                ArmaId = arma?.Id,
                MunicionLoteId = lote?.Id,
                Cantidad = item.Cantidad,
                PrecioUnitario = item.PrecioUnitario,
                Descuento = descuento,
                AlicuotaIva = producto.AlicuotaIva,
                Subtotal = baseImponible,
                Iva = iva,
                Total = baseImponible + iva
            });

            if (descuentaStock && errores.Count == 0)
            {
                DescontarStock(db, venta, producto, item, arma, lote, cliente.Id);
            }
        }

        if (errores.Count > 0)
        {
            throw new VentaValidationException(errores);
        }

        venta.Subtotal = venta.Detalles.Sum(x => x.Subtotal);
        venta.IvaTotal = venta.Detalles.Sum(x => x.Iva);
        venta.Total = venta.Subtotal + venta.IvaTotal - request.DescuentoGeneral;
        if (venta.Total < 0)
        {
            errores.Add("El total de la venta no puede ser negativo.");
            throw new VentaValidationException(errores);
        }

        db.Ventas.Add(venta);
        await db.SaveChangesAsync(cancellationToken);

        var comprobante = await facturacionElectronicaService.EmitirAsync(venta, cancellationToken);
        venta.PuntoVenta = comprobante.PuntoVenta;
        venta.NumeroComprobante = comprobante.NumeroComprobante;
        venta.Cae = comprobante.Cae;
        venta.CaeVencimiento = comprobante.CaeVencimiento;

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var advertencias = venta.Detalles
            .Select(x => productos[x.ProductoId])
            .Where(x => x.StockActual <= x.StockMinimo)
            .Select(x => $"{x.Nombre} quedó en stock mínimo ({x.StockActual}/{x.StockMinimo}).")
            .Distinct()
            .ToArray();

        return new VentaConfirmadaDto(
            venta.Id,
            venta.NumeroComprobante ?? "Sin comprobante",
            venta.Total,
            venta.Estado,
            advertencias);
    }

    private static void ValidarCredencial(Cliente cliente, bool contieneControlado, List<string> errores)
    {
        if (!contieneControlado)
        {
            return;
        }

        if (cliente.CredencialCLU is null)
        {
            errores.Add("El cliente no posee Credencial de Legítimo Usuario cargada.");
            return;
        }

        if (!cliente.CredencialCLU.EstaVigente)
        {
            errores.Add($"La CLU del cliente venció el {cliente.CredencialCLU.FechaVencimiento:dd/MM/yyyy}; no se puede vender armas ni municiones.");
        }
    }

    private static void ValidarItemBasico(ItemVentaRequest item, Producto producto, List<string> errores)
    {
        if (!producto.Activo)
        {
            errores.Add($"El producto {producto.Nombre} no está activo.");
        }

        if (item.Cantidad <= 0)
        {
            errores.Add($"La cantidad de {producto.Nombre} debe ser mayor a cero.");
        }

        if (item.PrecioUnitario <= 0)
        {
            errores.Add($"El precio de {producto.Nombre} debe ser mayor a cero.");
        }

        if (item.Descuento < 0)
        {
            errores.Add($"El descuento de {producto.Nombre} no puede ser negativo.");
        }

        if (producto.StockActual < item.Cantidad)
        {
            errores.Add($"Stock insuficiente para {producto.Nombre}. Disponible: {producto.StockActual}.");
        }
    }

    private static Arma? ValidarArma(
        ItemVentaRequest item,
        Producto producto,
        IReadOnlyDictionary<int, Arma> armas,
        List<string> errores)
    {
        if (item.Cantidad != 1)
        {
            errores.Add($"Las armas deben venderse individualmente por número de serie ({producto.Nombre}).");
        }

        if (item.ArmaId is null || !armas.TryGetValue(item.ArmaId.Value, out var arma))
        {
            errores.Add($"Debe seleccionar el número de serie del arma {producto.Nombre}.");
            return null;
        }

        if (arma.ProductoId != producto.Id)
        {
            errores.Add($"El número de serie {arma.NumeroSerie} no corresponde al producto {producto.Nombre}.");
        }

        if (arma.ClienteActualId is not null)
        {
            errores.Add($"El arma con serie {arma.NumeroSerie} ya se encuentra asignada a un cliente.");
        }

        if (arma.EstadoTramiteAnmac != EstadoTramiteAnmac.Autorizado)
        {
            errores.Add($"El arma con serie {arma.NumeroSerie} no está autorizada por ANMaC para la entrega.");
        }

        return arma;
    }

    private static MunicionLote? ValidarMunicion(
        ItemVentaRequest item,
        Producto producto,
        IReadOnlyDictionary<int, MunicionLote> lotes,
        IReadOnlySet<string> calibresHabilitados,
        List<string> errores)
    {
        if (item.MunicionLoteId is null || !lotes.TryGetValue(item.MunicionLoteId.Value, out var lote))
        {
            errores.Add($"Debe seleccionar el lote de munición para {producto.Nombre}.");
            return null;
        }

        if (lote.ProductoId != producto.Id)
        {
            errores.Add($"El lote {lote.NumeroLote} no corresponde al producto {producto.Nombre}.");
        }

        if (lote.CantidadDisponible < item.Cantidad)
        {
            errores.Add($"Stock insuficiente en lote {lote.NumeroLote}. Disponible: {lote.CantidadDisponible}.");
        }

        if (!calibresHabilitados.Contains(NormalizarCalibre(lote.Calibre)))
        {
            errores.Add($"El cliente no registra armas habilitantes para munición calibre {lote.Calibre}.");
        }

        return lote;
    }

    private static void DescontarStock(
        ApplicationDbContext db,
        Venta venta,
        Producto producto,
        ItemVentaRequest item,
        Arma? arma,
        MunicionLote? lote,
        int clienteId)
    {
        producto.StockActual -= item.Cantidad;

        if (arma is not null)
        {
            arma.ClienteActualId = clienteId;
            arma.EstadoTramiteAnmac = EstadoTramiteAnmac.Entregado;
            arma.FechaTransferencia = DateTime.UtcNow;
        }

        if (lote is not null)
        {
            lote.CantidadDisponible -= item.Cantidad;
        }

        db.MovimientosStock.Add(new MovimientoStock
        {
            ProductoId = producto.Id,
            Venta = venta,
            ArmaId = arma?.Id,
            MunicionLoteId = lote?.Id,
            Tipo = TipoMovimientoStock.EgresoVenta,
            Cantidad = -item.Cantidad,
            StockResultante = producto.StockActual,
            Observacion = "Venta confirmada"
        });
    }

    private static string NormalizarCalibre(string calibre) =>
        calibre.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
}
