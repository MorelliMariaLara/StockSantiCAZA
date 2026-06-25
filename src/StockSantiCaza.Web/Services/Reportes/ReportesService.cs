using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Data;
using StockSantiCaza.Web.Models;

namespace StockSantiCaza.Web.Services.Reportes;

public class ReportesService(IDbContextFactory<ApplicationDbContext> dbContextFactory) : IReportesService
{
    public async Task<DashboardResumenDto> ObtenerDashboardAsync(
        DateOnly fecha,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var desde = fecha.ToDateTime(TimeOnly.MinValue);
        var hasta = fecha.ToDateTime(TimeOnly.MaxValue);

        var ventasQuery = db.Ventas.AsNoTracking()
            .Where(x => x.Fecha >= desde && x.Fecha <= hasta && x.Estado != EstadoVenta.Anulada);

        var cantidadVentas = await ventasQuery.CountAsync(cancellationToken);
        var totalVentas = await ventasQuery.SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0m;
        var movimientos = await db.MovimientosStock.AsNoTracking()
            .CountAsync(x => x.Fecha >= desde && x.Fecha <= hasta, cancellationToken);

        var alertas = await db.Productos.AsNoTracking()
            .Where(x => x.Activo && x.StockActual <= x.StockMinimo)
            .OrderBy(x => x.StockActual)
            .ThenBy(x => x.Nombre)
            .Take(25)
            .Select(x => new StockAlertaDto(x.Sku, x.Nombre, x.StockActual, x.StockMinimo))
            .ToListAsync(cancellationToken);

        return new DashboardResumenDto(
            cantidadVentas,
            totalVentas,
            movimientos,
            alertas.Count,
            alertas);
    }

    public async Task<byte[]> ExportarVentasExcelAsync(
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var desdeDate = desde.ToDateTime(TimeOnly.MinValue);
        var hastaDate = hasta.ToDateTime(TimeOnly.MaxValue);

        var ventas = await db.Ventas.AsNoTracking()
            .Include(x => x.Cliente)
            .Include(x => x.Detalles)
                .ThenInclude(x => x.Producto)
            .Include(x => x.Detalles)
                .ThenInclude(x => x.Arma)
            .Include(x => x.Detalles)
                .ThenInclude(x => x.MunicionLote)
            .Where(x => x.Fecha >= desdeDate && x.Fecha <= hastaDate)
            .OrderBy(x => x.Fecha)
            .ToListAsync(cancellationToken);

        var stock = await db.Productos.AsNoTracking()
            .OrderBy(x => x.Categoria)
            .ThenBy(x => x.Nombre)
            .Select(x => new StockExportRow(
                x.Sku,
                x.Nombre,
                x.Categoria,
                x.Marca,
                x.Modelo,
                x.Calibre,
                x.PrecioUnitario,
                x.StockActual,
                x.StockMinimo,
                x.StockActual <= x.StockMinimo ? "ALERTA" : "OK"))
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        AgregarHojaVentas(workbook, ventas);
        AgregarHojaDetalle(workbook, ventas);
        AgregarHojaStock(workbook, stock);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void AgregarHojaVentas(XLWorkbook workbook, IReadOnlyList<Venta> ventas)
    {
        var ws = workbook.Worksheets.Add("Ventas");
        var headers = new[]
        {
            "Fecha", "Comprobante", "Tipo", "Cliente", "DNI/CUIT", "Vendedor", "Subtotal USD", "Descuento USD", "Total USD", "CAE"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }

        for (var row = 0; row < ventas.Count; row++)
        {
            var venta = ventas[row];
            var excelRow = row + 2;
            ws.Cell(excelRow, 1).Value = venta.Fecha;
            ws.Cell(excelRow, 2).Value = venta.NumeroComprobante;
            ws.Cell(excelRow, 3).Value = venta.TipoComprobante.ToString();
            ws.Cell(excelRow, 4).Value = venta.Cliente.NombreRazonSocial;
            ws.Cell(excelRow, 5).Value = venta.Cliente.DniCuit;
            ws.Cell(excelRow, 6).Value = FormatearVendedor(venta);
            ws.Cell(excelRow, 7).Value = venta.Subtotal;
            ws.Cell(excelRow, 8).Value = venta.DescuentoTotal;
            ws.Cell(excelRow, 9).Value = venta.Total;
            ws.Cell(excelRow, 10).Value = venta.Cae;
        }

        FormatearTabla(ws, headers.Length);
    }

    private static void AgregarHojaDetalle(XLWorkbook workbook, IReadOnlyList<Venta> ventas)
    {
        var ws = workbook.Worksheets.Add("Detalle");
        var headers = new[]
        {
            "Comprobante", "Vendedor", "SKU", "Producto", "Categoría", "Serie arma", "Lote munición", "Calibre", "Cantidad", "Precio USD", "Descuento USD", "Total USD"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }

        var row = 2;
        foreach (var detalle in ventas.SelectMany(x => x.Detalles, (venta, detalle) => new { venta, detalle }))
        {
            ws.Cell(row, 1).Value = detalle.venta.NumeroComprobante;
            ws.Cell(row, 2).Value = FormatearVendedor(detalle.venta);
            ws.Cell(row, 3).Value = detalle.detalle.Producto.Sku;
            ws.Cell(row, 4).Value = detalle.detalle.Producto.Nombre;
            ws.Cell(row, 5).Value = detalle.detalle.Producto.Categoria.ToString();
            ws.Cell(row, 6).Value = detalle.detalle.Arma?.NumeroSerie;
            ws.Cell(row, 7).Value = detalle.detalle.MunicionLote?.NumeroLote;
            ws.Cell(row, 8).Value = detalle.detalle.Arma?.Calibre ?? detalle.detalle.MunicionLote?.Calibre ?? detalle.detalle.Producto.Calibre;
            ws.Cell(row, 9).Value = detalle.detalle.Cantidad;
            ws.Cell(row, 10).Value = detalle.detalle.PrecioUnitario;
            ws.Cell(row, 11).Value = detalle.detalle.Descuento;
            ws.Cell(row, 12).Value = detalle.detalle.Total;
            row++;
        }

        FormatearTabla(ws, headers.Length);
    }

    private static void AgregarHojaStock(XLWorkbook workbook, IEnumerable<StockExportRow> stock)
    {
        var ws = workbook.Worksheets.Add("Stock");
        var headers = new[] { "SKU", "Producto", "Categoría", "Marca", "Modelo", "Calibre", "Precio USD", "Stock", "Mínimo", "Estado" };

        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }

        var row = 2;
        foreach (var item in stock)
        {
            ws.Cell(row, 1).Value = item.Sku;
            ws.Cell(row, 2).Value = item.Nombre;
            ws.Cell(row, 3).Value = item.Categoria.ToString();
            ws.Cell(row, 4).Value = item.Marca;
            ws.Cell(row, 5).Value = item.Modelo;
            ws.Cell(row, 6).Value = item.Calibre;
            ws.Cell(row, 7).Value = item.PrecioUnitario;
            ws.Cell(row, 8).Value = item.StockActual;
            ws.Cell(row, 9).Value = item.StockMinimo;
            ws.Cell(row, 10).Value = item.Estado;
            if (item.Estado == "ALERTA")
            {
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.LightPink;
            }
            row++;
        }

        FormatearTabla(ws, headers.Length);
    }

    private static string FormatearVendedor(Venta venta) =>
        string.IsNullOrWhiteSpace(venta.Vendedor) ? "Sin vendedor asignado" : venta.Vendedor;

    private static void FormatearTabla(IXLWorksheet ws, int columnas)
    {
        var rango = ws.Range(1, 1, Math.Max(ws.LastRowUsed()?.RowNumber() ?? 1, 1), columnas);
        rango.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        rango.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Row(1).Style.Font.Bold = true;
        ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1f3a5f");
        ws.Row(1).Style.Font.FontColor = XLColor.White;
        ws.Columns().AdjustToContents();
    }

    private sealed record StockExportRow(
        string Sku,
        string Nombre,
        ProductoCategoria Categoria,
        string? Marca,
        string? Modelo,
        string? Calibre,
        decimal PrecioUnitario,
        int StockActual,
        int StockMinimo,
        string Estado);
}
