using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Data;
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

    private static readonly string[] EncabezadosPlantilla =
    {
        "SKU", "Producto", "Categoría", "Marca", "Modelo", "Calibre", "Stock", "Mínimo", "Precio USD"
    };

    private static readonly (string Key, string[] Aliases)[] ColumnasConocidas =
    {
        ("sku", new[] { "sku", "codigo", "código" }),
        ("producto", new[] { "producto", "nombre", "descripcion", "descripción" }),
        ("categoria", new[] { "categoria", "categoría", "clasificacion", "clasificación" }),
        ("marca", new[] { "marca", "marc" }),
        ("modelo", new[] { "modelo" }),
        ("calibre", new[] { "calibre", "medida" }),
        ("stock", new[] { "stock", "existencia", "cantidad" }),
        ("minimo", new[] { "minimo", "mínimo", "minim", "mínim", "min" }),
        ("precio", new[] { "precio usd", "precio", "precio (usd)", "usd" })
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

        var columnas = ResolverColumnas(hoja.Row(1));
        ValidarColumnasRequeridas(columnas);

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var ultimaFila = hoja.LastRowUsed()?.RowNumber() ?? 1;

        for (var fila = 2; fila <= ultimaFila; fila++)
        {
            var sku = NormalizarCampoOpcional(LeerTexto(hoja, fila, columnas, "sku"), "SKU")?.ToUpperInvariant();
            var nombre = NormalizarCampoOpcional(LeerTexto(hoja, fila, columnas, "producto"), "Producto");
            var categoria = NormalizarCampoOpcional(LeerTexto(hoja, fila, columnas, "categoria"), "General", "Categoría", "Categoria") ?? "General";
            var marca = NormalizarCampoOpcional(LeerTexto(hoja, fila, columnas, "marca"), "Marc", "Marca");
            var modelo = NormalizarCampoOpcional(LeerTexto(hoja, fila, columnas, "modelo"), "Modelo");
            var calibre = Normalizar(LeerTexto(hoja, fila, columnas, "calibre"));
            var stock = ParseEntero(LeerCelda(hoja, fila, columnas, "stock"));
            var minimo = ParseEntero(LeerCelda(hoja, fila, columnas, "minimo"));
            var precio = ParsePrecio(LeerCelda(hoja, fila, columnas, "precio"));

            if (sku is null && nombre is null && marca is null && modelo is null)
            {
                continue;
            }

            if (nombre is null && sku is null)
            {
                errores.Add($"Fila {fila}: falta SKU o nombre de producto.");
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
            producto.Nombre = nombre ?? sku;
            producto.Categoria = categoria;
            producto.Marca = marca;
            producto.Modelo = modelo;
            producto.Calibre = calibre;
            producto.StockActual = stock;
            producto.StockMinimo = minimo > 0 ? minimo : 1;
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

        for (var i = 0; i < EncabezadosPlantilla.Length; i++)
        {
            ws.Cell(1, i + 1).Value = EncabezadosPlantilla[i];
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
        ws.Cell(2, 9).Style.NumberFormat.Format = "$#,##0.00";

        ws.Row(1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    internal static Dictionary<string, int> ResolverColumnas(IXLRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var ultimaColumna = headerRow.LastCellUsed()?.Address.ColumnNumber ?? EncabezadosPlantilla.Length;

        for (var col = 1; col <= ultimaColumna; col++)
        {
            var clave = ClasificarEncabezado(headerRow.Cell(col).GetString());
            if (clave is not null && !map.ContainsKey(clave))
            {
                map[clave] = col;
            }
        }

        if (!map.ContainsKey("producto") && ultimaColumna >= 2)
        {
            map.TryAdd("sku", 1);
            map.TryAdd("producto", 2);
            map.TryAdd("categoria", 3);
            map.TryAdd("marca", 4);
            map.TryAdd("modelo", 5);
            map.TryAdd("calibre", 6);
            map.TryAdd("stock", 7);
            map.TryAdd("minimo", 8);
            map.TryAdd("precio", 9);
        }

        return map;
    }

    internal static string? ClasificarEncabezado(string? encabezado)
    {
        var normalizado = NormalizarEncabezado(encabezado);
        if (string.IsNullOrWhiteSpace(normalizado))
        {
            return null;
        }

        foreach (var (key, aliases) in ColumnasConocidas)
        {
            if (aliases.Any(alias => normalizado.Equals(alias, StringComparison.OrdinalIgnoreCase)
                || normalizado.StartsWith(alias, StringComparison.OrdinalIgnoreCase)))
            {
                return key;
            }
        }

        return null;
    }

    internal static decimal ParsePrecio(IXLCell? cell)
    {
        if (cell is null || cell.IsEmpty())
        {
            return 0m;
        }

        if (cell.DataType == XLDataType.Number)
        {
            return Math.Max(0m, Convert.ToDecimal(cell.GetDouble(), CultureInfo.InvariantCulture));
        }

        var texto = cell.GetString().Trim();
        if (string.IsNullOrWhiteSpace(texto))
        {
            return 0m;
        }

        texto = texto
            .Replace("USD", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("$", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        var ultimaComa = texto.LastIndexOf(',');
        var ultimoPunto = texto.LastIndexOf('.');

        if (ultimaComa > ultimoPunto)
        {
            texto = texto.Replace(".", string.Empty, StringComparison.Ordinal).Replace(',', '.');
        }
        else if (ultimoPunto > ultimaComa)
        {
            var partes = texto.Split('.');
            if (partes.Length == 2)
            {
                texto = partes[0].Replace(",", string.Empty, StringComparison.Ordinal) + "." + partes[1];
            }
            else
            {
                texto = texto.Replace(",", string.Empty, StringComparison.Ordinal);
            }
        }
        else if (ultimaComa >= 0)
        {
            texto = texto.Replace(',', '.');
        }

        return decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var precio)
            ? Math.Max(0m, precio)
            : 0m;
    }

    internal static int ParseEntero(IXLCell? cell)
    {
        if (cell is null || cell.IsEmpty())
        {
            return 0;
        }

        if (cell.DataType == XLDataType.Number)
        {
            return Math.Max(0, (int)Math.Round(cell.GetDouble()));
        }

        var texto = cell.GetString().Trim();
        if (int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var entero))
        {
            return Math.Max(0, entero);
        }

        if (double.TryParse(texto.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var numero))
        {
            return Math.Max(0, (int)Math.Round(numero));
        }

        return 0;
    }

    private static void ValidarColumnasRequeridas(Dictionary<string, int> columnas)
    {
        if (!columnas.ContainsKey("producto") && !columnas.ContainsKey("sku"))
        {
            throw new InvalidOperationException(
                "No se reconocieron las columnas del Excel. Se requiere al menos SKU o Producto en la primera fila.");
        }

        if (!columnas.ContainsKey("stock") || !columnas.ContainsKey("precio"))
        {
            throw new InvalidOperationException(
                "No se reconocieron las columnas Stock y Precio USD. Verifique la primera fila del archivo.");
        }
    }

    private static IXLCell? LeerCelda(IXLWorksheet hoja, int fila, Dictionary<string, int> columnas, string clave) =>
        columnas.TryGetValue(clave, out var col) ? hoja.Cell(fila, col) : null;

    private static string? LeerTexto(IXLWorksheet hoja, int fila, Dictionary<string, int> columnas, string clave) =>
        Normalizar(LeerCelda(hoja, fila, columnas, clave)?.GetString());

    private static string NormalizarEncabezado(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        var sinAcentos = valor.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(sinAcentos.Length);
        foreach (var c in sinAcentos)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string? Normalizar(string? valor)
    {
        var normalizado = valor?.Trim();
        return string.IsNullOrWhiteSpace(normalizado) ? null : normalizado;
    }

    private static string? NormalizarCampoOpcional(string? valor, params string[] placeholders)
    {
        var normalizado = Normalizar(valor);
        if (normalizado is null)
        {
            return null;
        }

        return placeholders.Any(p => normalizado.Equals(p, StringComparison.OrdinalIgnoreCase))
            ? null
            : normalizado;
    }
}
