using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Data;
using StockSantiCaza.Web.Models;

namespace StockSantiCaza.Web.Services.Ventas;

public class VentasService : IVentasService
{
    private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;

    public VentasService(IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    public async Task<VentaConfirmadaDto> ConfirmarVentaAsync(
        NuevaVentaRequest request,
        CancellationToken cancellationToken = default)
    {
        var errores = new List<string>();
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

        await using var seedDb = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var strategy = seedDb.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            var erroresTransaccion = new List<string>();
            var advertencias = new List<string>();
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            var cliente = await db.Clientes
            .Include(x => x.CredencialCLU)
            .Include(x => x.ArmasRegistradas)
            .SingleOrDefaultAsync(x => x.Id == request.ClienteId, cancellationToken);

        if (cliente is null)
        {
            throw new VentaValidationException(new[] { "El cliente seleccionado no existe." });
        }

        if (!cliente.Activo)
        {
            throw new VentaValidationException(new[] { "El cliente seleccionado está dado de baja." });
        }

        var vendedor = await db.Usuarios
            .SingleOrDefaultAsync(x => x.Id == request.VendedorId && x.Activo, cancellationToken);

        if (vendedor is null)
        {
            throw new VentaValidationException(new[] { "El vendedor seleccionado no existe o está inactivo." });
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

        var categorias = await db.CategoriasStock
            .AsNoTracking()
            .Where(x => x.Activo)
            .ToListAsync(cancellationToken);

        var contieneControlado = request.Items.Any(item =>
            productos.TryGetValue(item.ProductoId, out var producto)
            && EsCategoriaControlada(producto.Categoria, categorias));

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
                erroresTransaccion.Add($"El producto con ID {item.ProductoId} no existe.");
                continue;
            }

            ValidarItemBasico(item, producto, erroresTransaccion);

            Arma? arma = null;
            MunicionLote? lote = null;
            var categoria = ObtenerCategoria(producto.Categoria, categorias);

            if (categoria?.RequiereSerie == true && item.ArmaId.HasValue)
            {
                arma = ValidarArma(item, producto, armas, erroresTransaccion);
            }
            else if (categoria?.RequiereLote == true && item.MunicionLoteId.HasValue)
            {
                lote = ValidarMunicion(item, producto, lotes, erroresTransaccion);
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

            if (erroresTransaccion.Count == 0)
            {
                DescontarStock(db, venta, producto, item, arma, lote, cliente.Id);
            }
        }

        if (erroresTransaccion.Count > 0)
        {
            throw new VentaValidationException(erroresTransaccion);
        }

        venta.Subtotal = venta.Detalles.Sum(x => x.Subtotal);
        venta.IvaTotal = 0;
        venta.Total = venta.Subtotal - request.DescuentoGeneral;
        if (venta.Total < 0)
        {
            erroresTransaccion.Add("El total de la venta no puede ser negativo.");
            throw new VentaValidationException(erroresTransaccion);
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
        });
    }

    public async Task EliminarAsync(
        int ventaId,
        int? usuarioActualId,
        bool esAdministrador,
        CancellationToken cancellationToken = default)
    {
        await using var seedDb = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var strategy = seedDb.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            var venta = await db.Ventas
            .Include(x => x.Detalles)
            .SingleOrDefaultAsync(x => x.Id == ventaId, cancellationToken);

        if (venta is null)
        {
            throw new VentaValidationException(new[] { "La venta seleccionada no existe." });
        }

        if (!esAdministrador && venta.VendedorId != usuarioActualId)
        {
            throw new VentaValidationException(new[] { "No tiene permiso para eliminar esta venta." });
        }

        var referencia = venta.NumeroComprobante ?? $"VTA-{venta.Id:D8}";

        foreach (var detalle in venta.Detalles)
        {
            var producto = await db.Productos.SingleAsync(x => x.Id == detalle.ProductoId, cancellationToken);
            producto.StockActual += detalle.Cantidad;

            if (detalle.ArmaId is int armaId)
            {
                var arma = await db.Armas.SingleAsync(x => x.Id == armaId, cancellationToken);
                arma.ClienteActualId = null;
                arma.EstadoTramiteAnmac = EstadoTramiteAnmac.Autorizado;
                arma.FechaTransferencia = null;
            }

            if (detalle.MunicionLoteId is int loteId)
            {
                var lote = await db.MunicionLotes.SingleAsync(x => x.Id == loteId, cancellationToken);
                lote.CantidadDisponible += detalle.Cantidad;
            }

            db.MovimientosStock.Add(new MovimientoStock
            {
                ProductoId = producto.Id,
                ArmaId = detalle.ArmaId,
                MunicionLoteId = detalle.MunicionLoteId,
                Tipo = TipoMovimientoStock.AnulacionVenta,
                Cantidad = detalle.Cantidad,
                StockResultante = producto.StockActual,
                Observacion = $"Anulación de venta {referencia}"
            });
        }

        var movimientosVenta = await db.MovimientosStock
            .Where(x => x.VentaId == ventaId)
            .ToListAsync(cancellationToken);
        db.MovimientosStock.RemoveRange(movimientosVenta);

        db.DetallesVenta.RemoveRange(venta.Detalles);
        db.Ventas.Remove(venta);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        });
    }

    private static CategoriaStock? ObtenerCategoria(string? nombre, IReadOnlyList<CategoriaStock> categorias)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return null;
        }

        return categorias.FirstOrDefault(x =>
            x.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));
    }

    private static bool EsCategoriaControlada(string? nombre, IReadOnlyList<CategoriaStock> categorias)
    {
        var categoria = ObtenerCategoria(nombre, categorias);
        return categoria?.RequiereSerie == true || categoria?.RequiereLote == true;
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

        if (cliente.CredencialCLU.FechaVencimiento is DateOnly vencimiento && vencimiento < DateOnly.FromDateTime(DateTime.Today))
        {
            advertencias.Add($"La CLU del cliente venció el {vencimiento:dd/MM/yyyy}; la venta se confirmó igualmente.");
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
