using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Data;
using StockSantiCaza.Web.Helpers;
using StockSantiCaza.Web.Models;
using StockSantiCaza.Web.Services.Auth;
using StockSantiCaza.Web.Services.Reportes;
using StockSantiCaza.Web.Services.Stock;
using StockSantiCaza.Web.Services.Usuarios;
using StockSantiCaza.Web.Services.Ventas;

namespace StockSantiCaza.Web.Controllers.Api;

[ApiController]
[Route("api/ventas")]
public class VentasController : ApiControllerBase
{
    private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;
    private readonly IVentasService ventasService;
    private readonly IUsuariosService usuariosService;

    public VentasController(
        IAuthService authService,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IVentasService ventasService,
        IUsuariosService usuariosService)
        : base(authService)
    {
        this.dbContextFactory = dbContextFactory;
        this.ventasService = ventasService;
        this.usuariosService = usuariosService;
    }

    [HttpGet("datos-nueva")]
    public async Task<ActionResult<NuevaVentaDatosDto>> DatosNuevaVenta(CancellationToken ct)
    {
        try
        {
            RequireModulo(ModuloSistema.Ventas);
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);

            var clientes = await db.Clientes
                .AsNoTracking()
                .Include(x => x.CredencialCLU)
                .Where(x => x.Activo)
                .OrderBy(x => x.NombreRazonSocial)
                .Select(x => new ClienteVentaDto(
                    x.Id,
                    x.NombreRazonSocial ?? string.Empty,
                    x.DniCuit ?? string.Empty,
                    x.Telefono,
                    x.Email))
                .ToListAsync(ct);

            var productos = await db.Productos
                .AsNoTracking()
                .Where(x => x.Activo)
                .OrderBy(x => x.Nombre)
                .Select(x => new ProductoVentaDto(
                    x.Id,
                    x.Sku ?? string.Empty,
                    x.Nombre ?? string.Empty,
                    x.Marca,
                    x.Calibre,
                    x.StockActual,
                    x.StockMinimo,
                    x.PrecioUnitario))
                .ToListAsync(ct);

            var vendedores = (await usuariosService.ListarVendedoresActivosAsync(ct))
                .Select(x => new VendedorDto(x.Id, x.Nombre, x.Rol.ToString()))
                .ToList();

            return Ok(new NuevaVentaDatosDto(clientes, productos, vendedores));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<VentaDto>>> Listar(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        CancellationToken ct)
    {
        try
        {
            var usuario = RequireModulo(ModuloSistema.Ventas);
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);

            var query = db.Ventas
                .AsNoTracking()
                .Include(x => x.Cliente)
                .Include(x => x.Detalles).ThenInclude(x => x.Producto)
                .Include(x => x.Detalles).ThenInclude(x => x.Arma)
                .Include(x => x.Detalles).ThenInclude(x => x.MunicionLote)
                .AsQueryable();

            if (usuario.EsAdministrador)
            {
                if (desde.HasValue && hasta.HasValue)
                {
                    if (desde.Value.Date > hasta.Value.Date)
                    {
                        return BadRequest(new { error = "La fecha desde no puede ser posterior a la fecha hasta." });
                    }

                    var desdeDt = desde.Value.Date;
                    var hastaDt = hasta.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(x => x.Fecha >= desdeDt && x.Fecha <= hastaDt);
                }
            }
            else
            {
                query = query
                    .Where(x => x.VendedorId == usuario.Id)
                    .OrderByDescending(x => x.Fecha)
                    .Take(100);
            }

            var ventas = await query
                .OrderByDescending(x => x.Fecha)
                .Take(500)
                .ToListAsync(ct);

            return Ok(ventas.Select(VentaDto.From).ToList());
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult<VentaConfirmadaResponse>> Confirmar([FromBody] NuevaVentaRequest request, CancellationToken ct)
    {
        try
        {
            RequireModulo(ModuloSistema.Ventas);
            var resultado = await ventasService.ConfirmarVentaAsync(request, ct);
            return Ok(new VentaConfirmadaResponse(
                resultado.VentaId,
                resultado.NumeroComprobante,
                resultado.Total,
                resultado.Estado.ToString(),
                resultado.Advertencias));
        }
        catch (VentaValidationException ex)
        {
            return BadRequest(new { errors = ex.Errores });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        try
        {
            var usuario = RequireModulo(ModuloSistema.Ventas);
            await ventasService.EliminarAsync(id, usuario.Id, usuario.EsAdministrador, ct);
            return Ok();
        }
        catch (VentaValidationException ex)
        {
            return BadRequest(new { errors = ex.Errores });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public sealed record NuevaVentaDatosDto(
        List<ClienteVentaDto> Clientes,
        List<ProductoVentaDto> Productos,
        List<VendedorDto> Vendedores);

    public sealed record ClienteVentaDto(int Id, string NombreRazonSocial, string DniCuit, string? Telefono, string? Email);
    public sealed record ProductoVentaDto(int Id, string Sku, string Nombre, string? Marca, string? Calibre, int StockActual, int StockMinimo, decimal PrecioUnitario);
    public sealed record VendedorDto(int Id, string Nombre, string Rol);

    public sealed record VentaConfirmadaResponse(
        int VentaId,
        string NumeroComprobante,
        decimal Total,
        string Estado,
        IReadOnlyList<string> Advertencias);

    public sealed record VentaDto(
        int Id,
        DateTime Fecha,
        string? NumeroComprobante,
        string Estado,
        decimal Total,
        string? Observaciones,
        int? VendedorId,
        string Vendedor,
        ClienteVentaDto Cliente,
        List<DetalleVentaDto> Detalles)
    {
        public static VentaDto From(Venta v) => new(
            v.Id,
            v.Fecha,
            v.NumeroComprobante,
            v.Estado.ToString(),
            v.Total,
            v.Observaciones,
            v.VendedorId,
            string.IsNullOrWhiteSpace(v.Vendedor) ? "Sin vendedor asignado" : v.Vendedor,
            new ClienteVentaDto(
                v.Cliente.Id,
                v.Cliente.NombreRazonSocial ?? string.Empty,
                v.Cliente.DniCuit ?? string.Empty,
                v.Cliente.Telefono,
                v.Cliente.Email),
            v.Detalles.OrderBy(x => x.Producto.Nombre).Select(DetalleVentaDto.From).ToList());
    }

    public sealed record DetalleVentaDto(
        string ProductoNombre,
        string ProductoSku,
        string? Categoria,
        string Trazabilidad,
        int Cantidad,
        decimal PrecioUnitario,
        decimal Total)
    {
        public static DetalleVentaDto From(DetalleVenta d)
        {
            var trazabilidad = d.Arma is not null
                ? $"Serie {d.Arma.NumeroSerie} - {d.Arma.Marca} {d.Arma.Modelo} - Calibre {d.Arma.Calibre}"
                : d.MunicionLote is not null
                    ? $"Lote {d.MunicionLote.NumeroLote} - Calibre {d.MunicionLote.Calibre}"
                    : "No aplica";

            return new(
                d.Producto.Nombre ?? string.Empty,
                d.Producto.Sku ?? string.Empty,
                d.Producto.Categoria,
                trazabilidad,
                d.Cantidad,
                d.PrecioUnitario,
                d.Total);
        }
    }
}

[ApiController]
[Route("api/reportes")]
public class ReportesController : ApiControllerBase
{
    private readonly IReportesService reportesService;

    public ReportesController(IAuthService authService, IReportesService reportesService)
        : base(authService)
    {
        this.reportesService = reportesService;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardResumenDto>> Dashboard([FromQuery] string? fecha, CancellationToken ct)
    {
        try
        {
            RequireAdmin();
            var fechaConsulta = FechaQueryHelper.ParseOpcional(fecha) ?? DateOnly.FromDateTime(DateTime.Today);
            var resumen = await reportesService.ObtenerDashboardAsync(fechaConsulta, ct);
            return Ok(resumen);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("periodo")]
    public async Task<ActionResult<ReportePeriodoDto>> Periodo(
        [FromQuery] string desde,
        [FromQuery] string hasta,
        CancellationToken ct)
    {
        try
        {
            RequireAdmin();
            var desdeFecha = FechaQueryHelper.ParseRequerida(desde, "desde");
            var hastaFecha = FechaQueryHelper.ParseRequerida(hasta, "hasta");

            if (desdeFecha > hastaFecha)
            {
                return BadRequest(new { error = "La fecha desde no puede ser posterior a la fecha hasta." });
            }

            var resumen = await reportesService.ObtenerReportePeriodoAsync(desdeFecha, hastaFecha, ct);
            return Ok(resumen);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("excel")]
    public async Task<IActionResult> Excel(
        [FromQuery] string desde,
        [FromQuery] string hasta,
        CancellationToken ct)
    {
        try
        {
            RequireAdmin();
            var desdeFecha = FechaQueryHelper.ParseRequerida(desde, "desde");
            var hastaFecha = FechaQueryHelper.ParseRequerida(hasta, "hasta");

            if (desdeFecha > hastaFecha)
            {
                return BadRequest(new { error = "La fecha desde no puede ser posterior a la fecha hasta." });
            }

            var bytes = await reportesService.ExportarVentasExcelAsync(desdeFecha, hastaFecha, ct);
            var fileName = $"ventas-stock-{desdeFecha:yyyyMMdd}-{hastaFecha:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}

[ApiController]
[Route("api/usuarios")]
public class UsuariosController : ApiControllerBase
{
    private readonly IUsuariosService usuariosService;

    public UsuariosController(IAuthService authService, IUsuariosService usuariosService)
        : base(authService)
    {
        this.usuariosService = usuariosService;
    }

    [HttpGet]
    public async Task<ActionResult<List<UsuarioDto>>> Listar(CancellationToken ct)
    {
        try
        {
            RequireAdmin();
            var usuarios = await usuariosService.ListarAsync(ct);
            return Ok(usuarios.Select(UsuarioDto.From).ToList());
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Guardar([FromBody] UsuarioFormRequest request, CancellationToken ct)
    {
        try
        {
            RequireAdmin();
            await usuariosService.GuardarAsync(request, ct);
            return Ok();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        try
        {
            var usuario = RequireAdmin();
            await usuariosService.EliminarAsync(id, usuario.Id, ct);
            return Ok();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public sealed record UsuarioDto(int Id, string Nombre, string Login, string Rol, bool Activo)
    {
        public static UsuarioDto From(Usuario u) => new(u.Id, u.Nombre, u.Login, u.Rol.ToString(), u.Activo);
    }
}
