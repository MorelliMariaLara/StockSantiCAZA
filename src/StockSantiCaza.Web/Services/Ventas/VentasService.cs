using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Data;
using StockSantiCaza.Web.Models;

namespace StockSantiCaza.Web.Services.Ventas;

public class VentasService(IDbContextFactory<ApplicationDbContext> dbContextFactory) : IVentasService
{
    public async Task<VentaConfirmadaDto> ConfirmarVentaAsync(
        NuevaVentaRequest request,
        CancellationToken cancellationToken = default)
    {
        var errores = new List<string>();
        var advertencias = new List<string>();
        if (request.ClienteId is null)
        {
            errores.Add("Debe seleccionar un cliente.");
        }

        if (request.VendedorId is null)
        {
            errores.Add("Debe seleccionar el vendedor que realizó la venta.");
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

        if (!cliente.Activo)
        {
            throw new VentaValidationException(["El cliente seleccionado está dado de baja."]);
        }

        var vendedor = await db.Usuarios
            .SingleOrDefaultAsync(x => x.Id == request.VendedorId && x.Activo, cancellationToken);

        if (vendedor is null)
        {
            throw new VentaValidationException(["El vendedor seleccionado no existe o está inactivo."]);
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

        RegistrarAdvertenciaCredencial(cliente, contieneControlado, advertencias);

        var venta = new Venta
        {
            ClienteId = cliente.Id,
            VendedorId = vendedor.Id,
            Vendedor = vendedor.Nombre,
            TipoComprobante = TipoComprobante.Presupuesto,
            Estado = EstadoVenta.Confirmada,
            DescuentoTotal = request.DescuentoGeneral,
            Observaciones = request.Observaciones,
            Fecha = DateTime.UtcNow
        };

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

            if (producto.Categoria == ProductoCategoria.Arma && item.ArmaId.HasValue)
            {
                arma = ValidarArma(item, producto, armas, errores);
            }
            else if (producto.Categoria == ProductoCategoria.Municion && item.MunicionLoteId.HasValue)
            {
                lote = ValidarMunicion(item, producto, lotes, errores);
            }

            var lineaBruta = item.PrecioUnitario * item.Cantidad;
            var descuento = Math.Min(item.Descuento, lineaBruta);
            var subtotal = lineaBruta - descuento;

            venta.Detalles.Add(new DetalleVenta
            {
                ProductoId = producto.Id,
                ArmaId = arma?.Id,
                MunicionLoteId = lote?.Id,
                Cantidad = item.Cantidad,
                PrecioUnitario = item.PrecioUnitario,
                Descuento = descuento,
                AlicuotaIva = 0,
                Subtotal = subtotal,
                Iva = 0,
                Total = subtotal
            });

            if (errores.Count == 0)
            {
                DescontarStock(db, venta, producto, item, arma, lote, cliente.Id);
            }
        }

        if (errores.Count > 0)
        {
            throw new VentaValidationException(errores);
        }

        venta.Subtotal = venta.Detalles.Sum(x => x.Subtotal);
        venta.IvaTotal = 0;
        venta.Total = venta.Subtotal - request.DescuentoGeneral;
        if (venta.Total < 0)
        {
            errores.Add("El total de la venta no puede ser negativo.");
            throw new VentaValidationException(errores);
        }

        db.Ventas.Add(venta);
        await db.SaveChangesAsync(cancellationToken);

        venta.NumeroComprobante = $"VTA-{venta.Id:D8}";

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        advertencias.AddRange(venta.Detalles
            .Select(x => productos[x.ProductoId])
            .Where(x => x.StockActual <= x.StockMinimo)
            .Select(x => $"{x.Nombre} quedó en stock mínimo ({x.StockActual}/{x.StockMinimo}).")
            .Distinct());

        return new VentaConfirmadaDto(
            venta.Id,
            venta.NumeroComprobante ?? "Sin número",
            venta.Total,
            venta.Estado,
            advertencias);
    }

    private static void RegistrarAdvertenciaCredencial(Cliente cliente, bool contieneControlado, List<string> advertencias)
    {
        if (!contieneControlado)
        {
            return;
        }

        if (cliente.CredencialCLU is null)
        {
            advertencias.Add("El cliente no posee Credencial de Legítimo Usuario cargada; la venta se confirmó igualmente.");
            return;
        }

        if (!cliente.CredencialCLU.EstaVigente)
        {
            advertencias.Add($"La CLU del cliente venció el {cliente.CredencialCLU.FechaVencimiento:dd/MM/yyyy}; la venta se confirmó igualmente.");
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
        if (item.ArmaId is null)
        {
            return null;
        }

        if (!armas.TryGetValue(item.ArmaId.Value, out var arma))
        {
            errores.Add($"El arma seleccionada para {producto.Nombre} no existe.");
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
        List<string> errores)
    {
        if (item.MunicionLoteId is null)
        {
            return null;
        }

        if (!lotes.TryGetValue(item.MunicionLoteId.Value, out var lote))
        {
            errores.Add($"El lote seleccionado para {producto.Nombre} no existe.");
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
}
