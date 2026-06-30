using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Data;
using StockSantiCaza.Web.Helpers;
using StockSantiCaza.Web.Models;
using StockSantiCaza.Web.Services.Auth;
using StockSantiCaza.Web.Services.Stock;

namespace StockSantiCaza.Web.Controllers.Api;

[ApiController]
[Route("api/stock")]
public class StockController : ApiControllerBase
{
    private const long MaxTamanoArchivoImportacion = 10 * 1024 * 1024;

    private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;
    private readonly IStockImportService importService;

    public StockController(
        IAuthService authService,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IStockImportService importService)
        : base(authService)
    {
        this.dbContextFactory = dbContextFactory;
        this.importService = importService;
    }

    [HttpGet]
    public async Task<ActionResult<StockDatosDto>> ObtenerDatos(CancellationToken ct)
    {
        try
        {
            var usuario = RequireModulo(ModuloSistema.Stock);
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);

            var categorias = await db.CategoriasStock
                .AsNoTracking()
                .Where(x => x.Activo)
                .OrderBy(x => x.Nombre)
                .Select(x => new CategoriaDto(x.Id, x.Nombre, x.RequiereSerie, x.RequiereLote))
                .ToListAsync(ct);

            var productos = await db.Productos
                .AsNoTracking()
                .OrderBy(x => x.Nombre)
                .Select(x => new ProductoDto(
                    x.Id,
                    x.Sku,
                    x.Nombre,
                    x.Descripcion,
                    x.Categoria,
                    x.Marca,
                    x.Modelo,
                    x.Calibre,
                    x.PrecioUnitario,
                    x.StockActual,
                    x.StockMinimo))
                .ToListAsync(ct);

            return Ok(new StockDatosDto(productos, categorias, usuario.PuedeEditarStock));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("productos")]
    public async Task<ActionResult<ProductoDto>> GuardarProducto([FromBody] ProductoFormRequest request, CancellationToken ct)
    {
        try
        {
            RequireModulo(ModuloSistema.Stock);
            var usuario = AuthService.UsuarioActual!;
            if (!usuario.PuedeEditarStock)
            {
                return Unauthorized(new { error = "Solo administradores pueden editar stock." });
            }

            var errores = new List<string>();
            var sku = DisplayHelper.NormalizarOpcional(request.Sku)?.ToUpperInvariant();
            var nombre = DisplayHelper.NormalizarOpcional(request.Nombre);
            var categoria = DisplayHelper.NormalizarOpcional(request.Categoria);

            if (request.StockActual < 0) request.StockActual = 0;
            if (request.StockMinimo < 0) request.StockMinimo = 0;

            await using var db = await dbContextFactory.CreateDbContextAsync(ct);

            if (!string.IsNullOrWhiteSpace(sku))
            {
                var skuDuplicado = request.Id is int productoActualId
                    ? await db.Productos.AnyAsync(x => x.Sku == sku && x.Id != productoActualId, ct)
                    : await db.Productos.AnyAsync(x => x.Sku == sku, ct);
                if (skuDuplicado)
                {
                    errores.Add($"Ya existe un producto con SKU {sku}.");
                }
            }

            if (errores.Count > 0)
            {
                return BadRequest(new { errors = errores });
            }

            Producto producto;
            var stockAnterior = 0;
            var esNuevo = request.Id is null;

            if (esNuevo)
            {
                producto = new Producto { CreadoEn = DateTime.UtcNow };
                db.Productos.Add(producto);
            }
            else
            {
                var productoExistente = await db.Productos.SingleOrDefaultAsync(x => x.Id == request.Id, ct);
                if (productoExistente is null)
                {
                    return BadRequest(new { errors = new[] { "El producto seleccionado ya no existe." } });
                }

                producto = productoExistente;
                stockAnterior = producto.StockActual;
            }

            producto.Sku = sku;
            producto.Nombre = nombre;
            producto.Descripcion = DisplayHelper.NormalizarOpcional(request.Descripcion);
            producto.Categoria = categoria;
            producto.Marca = DisplayHelper.NormalizarOpcional(request.Marca);
            producto.Modelo = DisplayHelper.NormalizarOpcional(request.Modelo);
            producto.Calibre = DisplayHelper.NormalizarOpcional(request.Calibre);
            producto.PrecioUnitario = request.PrecioUnitario;
            producto.StockActual = request.StockActual;
            producto.StockMinimo = request.StockMinimo;
            producto.Activo = true;

            var diferenciaStock = producto.StockActual - stockAnterior;
            if (diferenciaStock != 0)
            {
                db.MovimientosStock.Add(new MovimientoStock
                {
                    Producto = producto,
                    Tipo = esNuevo && diferenciaStock > 0 ? TipoMovimientoStock.Ingreso : TipoMovimientoStock.Ajuste,
                    Cantidad = diferenciaStock,
                    StockResultante = producto.StockActual,
                    Observacion = esNuevo ? "Ingreso inicial desde módulo Stock" : "Ajuste manual desde módulo Stock"
                });
            }

            await db.SaveChangesAsync(ct);

            return Ok(new ProductoDto(
                producto.Id,
                producto.Sku,
                producto.Nombre,
                producto.Descripcion,
                producto.Categoria,
                producto.Marca,
                producto.Modelo,
                producto.Calibre,
                producto.PrecioUnitario,
                producto.StockActual,
                producto.StockMinimo));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpDelete("productos/{id:int}")]
    public async Task<IActionResult> EliminarProducto(int id, CancellationToken ct)
    {
        try
        {
            RequireModulo(ModuloSistema.Stock);
            var usuario = AuthService.UsuarioActual!;
            if (!usuario.PuedeEditarStock)
            {
                return Unauthorized(new { error = "Solo administradores pueden editar stock." });
            }

            await using var db = await dbContextFactory.CreateDbContextAsync(ct);

            var enVentas = await db.DetallesVenta.AnyAsync(x => x.ProductoId == id, ct);
            if (enVentas)
            {
                return BadRequest(new { error = "No se puede eliminar el producto porque está asociado a ventas registradas." });
            }

            var productoDb = await db.Productos.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (productoDb is null)
            {
                return NotFound(new { error = "El producto seleccionado ya no existe." });
            }

            var armaIds = await db.Armas.Where(x => x.ProductoId == id).Select(x => x.Id).ToListAsync(ct);
            if (armaIds.Count > 0 && await db.DetallesVenta.AnyAsync(x => x.ArmaId.HasValue && armaIds.Contains(x.ArmaId.Value), ct))
            {
                return BadRequest(new { error = "No se puede eliminar el producto porque tiene armas vinculadas a ventas." });
            }

            var loteIds = await db.MunicionLotes.Where(x => x.ProductoId == id).Select(x => x.Id).ToListAsync(ct);
            if (loteIds.Count > 0 && await db.DetallesVenta.AnyAsync(x => x.MunicionLoteId.HasValue && loteIds.Contains(x.MunicionLoteId.Value), ct))
            {
                return BadRequest(new { error = "No se puede eliminar el producto porque tiene lotes vinculados a ventas." });
            }

            var movimientos = await db.MovimientosStock.Where(x => x.ProductoId == id).ToListAsync(ct);
            db.MovimientosStock.RemoveRange(movimientos);

            var armas = await db.Armas.Where(x => x.ProductoId == id).ToListAsync(ct);
            db.Armas.RemoveRange(armas);

            var lotes = await db.MunicionLotes.Where(x => x.ProductoId == id).ToListAsync(ct);
            db.MunicionLotes.RemoveRange(lotes);

            db.Productos.Remove(productoDb);
            await db.SaveChangesAsync(ct);
            return Ok();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("categorias")]
    public async Task<ActionResult<CategoriaDto>> AgregarCategoria([FromBody] CategoriaFormRequest request, CancellationToken ct)
    {
        try
        {
            RequireModulo(ModuloSistema.Stock);
            var usuario = AuthService.UsuarioActual!;
            if (!usuario.PuedeEditarStock)
            {
                return Unauthorized(new { error = "Solo administradores pueden editar stock." });
            }

            var nombre = DisplayHelper.NormalizarOpcional(request.Nombre);
            if (nombre is null)
            {
                return BadRequest(new { error = "Debe indicar el nombre de la clasificación." });
            }

            await using var db = await dbContextFactory.CreateDbContextAsync(ct);
            var existe = await db.CategoriasStock.AnyAsync(x => x.Nombre.ToLower() == nombre.ToLower(), ct);
            if (existe)
            {
                return BadRequest(new { error = $"Ya existe la clasificación {nombre}." });
            }

            var categoria = new CategoriaStock
            {
                Nombre = nombre,
                RequiereSerie = request.RequiereSerie,
                RequiereLote = request.RequiereLote,
                Activo = true
            };
            db.CategoriasStock.Add(categoria);
            await db.SaveChangesAsync(ct);

            return Ok(new CategoriaDto(categoria.Id, categoria.Nombre, categoria.RequiereSerie, categoria.RequiereLote));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpDelete("categorias/{id:int}")]
    public async Task<IActionResult> EliminarCategoria(int id, CancellationToken ct)
    {
        try
        {
            RequireModulo(ModuloSistema.Stock);
            var usuario = AuthService.UsuarioActual!;
            if (!usuario.PuedeEditarStock)
            {
                return Unauthorized(new { error = "Solo administradores pueden editar stock." });
            }

            await using var db = await dbContextFactory.CreateDbContextAsync(ct);
            var categoria = await db.CategoriasStock.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (categoria is null)
            {
                return NotFound();
            }

            var enUso = await db.Productos.AnyAsync(x => x.Categoria == categoria.Nombre, ct);
            if (enUso)
            {
                return BadRequest(new { error = "No se puede eliminar la clasificación porque hay productos que la usan." });
            }

            db.CategoriasStock.Remove(categoria);
            await db.SaveChangesAsync(ct);
            return Ok();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("plantilla")]
    public ActionResult DescargarPlantilla()
    {
        try
        {
            RequireModulo(ModuloSistema.Stock);
            var usuario = AuthService.UsuarioActual!;
            if (!usuario.PuedeEditarStock)
            {
                return Unauthorized(new { error = "Solo administradores pueden editar stock." });
            }

            var bytes = StockImportService.GenerarPlantilla();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-stock.xlsx");
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("importar")]
    public async Task<ActionResult<StockImportResult>> ImportarExcel(IFormFile archivo, CancellationToken ct)
    {
        try
        {
            RequireModulo(ModuloSistema.Stock);
            var usuario = AuthService.UsuarioActual!;
            if (!usuario.PuedeEditarStock)
            {
                return Unauthorized(new { error = "Solo administradores pueden editar stock." });
            }

            if (archivo is null || archivo.Length == 0)
            {
                return BadRequest(new { error = "Debe seleccionar un archivo Excel." });
            }

            if (archivo.Length > MaxTamanoArchivoImportacion)
            {
                return BadRequest(new { error = "El archivo supera el tamaño máximo permitido (10 MB)." });
            }

            await using var stream = archivo.OpenReadStream();
            var resultado = await importService.ImportarAsync(stream, ct);
            return Ok(resultado);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public sealed record StockDatosDto(List<ProductoDto> Productos, List<CategoriaDto> Categorias, bool PuedeEditar);

    public sealed record ProductoDto(
        int Id, string? Sku, string? Nombre, string? Descripcion, string? Categoria,
        string? Marca, string? Modelo, string? Calibre, decimal PrecioUnitario,
        int StockActual, int StockMinimo);

    public sealed record CategoriaDto(int Id, string Nombre, bool RequiereSerie, bool RequiereLote);

    public sealed class ProductoFormRequest
    {
        public int? Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string Calibre { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }
        public int StockActual { get; set; }
        public int StockMinimo { get; set; } = 1;
    }

    public sealed class CategoriaFormRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public bool RequiereSerie { get; set; }
        public bool RequiereLote { get; set; }
    }
}
