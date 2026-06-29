using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Data;
using StockSantiCaza.Web.Helpers;
using StockSantiCaza.Web.Models;

namespace StockSantiCaza.Web.Services.Stock;

public interface IStockImportService
{
    Task<StockImportResult> ImportarAsync(Stream archivo, CancellationToken cancellationToken = default);
}

public sealed record StockImportResult(
    int Creados,
    int Actualizados,
    IReadOnlyList<string> Errores);

public class StockImportService : IStockImportService
{
    private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;

    private static readonly string[] EncabezadosEsperados =
    {
        "SKU", "Producto", "Categoría", "Marca", "Modelo", "Calibre", "Stock", "Mínimo", "Precio USD"
    };

    public StockImportService(IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    public async Task<StockImportResult> ImportarAsync(Stream archivo, CancellationToken cancellationToken = default)
    {
        var errores = new List<string>();
        var creados = 0;
        var actualizados = 0;

        using var workbook = new XLWorkbook(archivo);
        var hoja = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("El archivo Excel no contiene hojas.");

        var primeraFila = hoja.Row(1);
        for (var i = 0; i < EncabezadosEsperados.Length; i++)
        {
            var valor = primeraFila.Cell(i + 1).GetString().Trim();
            if (!valor.Equals(EncabezadosEsperados[i], StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Encabezado inválido en columna {i + 1}. Se esperaba '{EncabezadosEsperados[i]}'.");
            }
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var ultimaFila = hoja.LastRowUsed()?.RowNumber() ?? 1;

        for (var fila = 2; fila <= ultimaFila; fila++)
        {
            var sku = Normalizar(hoja.Cell(fila, 1).GetString())?.ToUpperInvariant();
            var nombre = Normalizar(hoja.Cell(fila, 2).GetString());
            var categoria = Normalizar(hoja.Cell(fila, 3).GetString()) ?? "General";
            var marca = Normalizar(hoja.Cell(fila, 4).GetString());
            var modelo = Normalizar(hoja.Cell(fila, 5).GetString());
            var calibre = Normalizar(hoja.Cell(fila, 6).GetString());
            var stock = Math.Max(0, (int)hoja.Cell(fila, 7).GetDouble());
            var minimo = Math.Max(0, (int)hoja.Cell(fila, 8).GetDouble());
            var precio = Math.Max(0m, (decimal)hoja.Cell(fila, 9).GetDouble());

            if (sku is null && nombre is null && marca is null && modelo is null)
            {
                continue;
            }

            Producto? producto = null;
            if (!string.IsNullOrWhiteSpace(sku))
            {
                producto = await db.Productos.SingleOrDefaultAsync(x => x.Sku == sku, cancellationToken);
            }

            var esNuevo = producto is null;
            var stockAnterior = producto?.StockActual ?? 0;

            producto ??= new Producto { CreadoEn = DateTime.UtcNow };
            producto.Sku = sku;
            producto.Nombre = nombre;
            producto.Categoria = categoria;
            producto.Marca = marca;
            producto.Modelo = modelo;
            producto.Calibre = calibre;
            producto.StockActual = stock;
            producto.StockMinimo = minimo;
            producto.PrecioUnitario = precio;
            producto.Activo = true;

            if (esNuevo)
            {
                db.Productos.Add(producto);
                creados++;
            }
            else
            {
                actualizados++;
            }

            var diferencia = producto.StockActual - stockAnterior;
            if (diferencia != 0)
            {
                db.MovimientosStock.Add(new MovimientoStock
                {
                    Producto = producto,
                    Tipo = esNuevo ? TipoMovimientoStock.Ingreso : TipoMovimientoStock.Ajuste,
                    Cantidad = diferencia,
                    StockResultante = producto.StockActual,
                    Observacion = esNuevo ? "Ingreso inicial por importación Excel" : "Ajuste por importación Excel"
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return new StockImportResult(creados, actualizados, errores);
    }

    public static byte[] GenerarPlantilla()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Stock");

        for (var i = 0; i < EncabezadosEsperados.Length; i++)
        {
            ws.Cell(1, i + 1).Value = EncabezadosEsperados[i];
        }

        ws.Cell(2, 1).Value = "SKU-001";
        ws.Cell(2, 2).Value = "Producto ejemplo";
        ws.Cell(2, 3).Value = "General";
        ws.Cell(2, 4).Value = "Marca";
        ws.Cell(2, 5).Value = "Modelo";
        ws.Cell(2, 6).Value = "9mm";
        ws.Cell(2, 7).Value = 10;
        ws.Cell(2, 8).Value = 2;
        ws.Cell(2, 9).Value = 150m;

        ws.Row(1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string? Normalizar(string? valor)
    {
        var normalizado = valor?.Trim();
        return string.IsNullOrWhiteSpace(normalizado) ? null : normalizado;
    }
}
