using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Data;
using StockSantiCaza.Web.Helpers;
using StockSantiCaza.Web.Models;
using StockSantiCaza.Web.Services.Auth;

namespace StockSantiCaza.Web.Controllers.Api;

[ApiController]
[Route("api/proveedores")]
public class ProveedoresController : ApiControllerBase
{
    private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;

    public ProveedoresController(IAuthService authService, IDbContextFactory<ApplicationDbContext> dbContextFactory)
        : base(authService)
    {
        this.dbContextFactory = dbContextFactory;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProveedorDto>>> Listar(CancellationToken ct)
    {
        try
        {
            RequireModulo(ModuloSistema.Proveedores);
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);
            var proveedores = await db.Proveedores
                .AsNoTracking()
                .Include(x => x.Movimientos)
                .OrderBy(x => x.NombreRazonSocial)
                .ToListAsync(ct);

            return Ok(proveedores.Select(ProveedorDto.From).ToList());
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult<ProveedorDto>> Guardar([FromBody] ProveedorFormRequest request, CancellationToken ct)
    {
        try
        {
            RequireModulo(ModuloSistema.Proveedores);
            var nombre = DisplayHelper.NormalizarTexto(request.NombreRazonSocial);

            if (request.Id is null && request.DeudaInicial < 0)
            {
                return BadRequest(new { errors = new[] { "La deuda inicial no puede ser negativa." } });
            }

            await using var db = await dbContextFactory.CreateDbContextAsync(ct);

            if (request.Id is null)
            {
                var proveedor = new Proveedor
                {
                    NombreRazonSocial = nombre,
                    Telefono = DisplayHelper.NormalizarOpcional(request.Telefono),
                    Email = DisplayHelper.NormalizarOpcional(request.Email),
                    Domicilio = DisplayHelper.NormalizarOpcional(request.Domicilio),
                    Observaciones = DisplayHelper.NormalizarOpcional(request.Observaciones),
                    Activo = true
                };

                if (request.DeudaInicial > 0)
                {
                    proveedor.Movimientos.Add(new MovimientoProveedor
                    {
                        Tipo = TipoMovimientoProveedor.Deuda,
                        Fecha = DateTime.UtcNow,
                        Monto = request.DeudaInicial,
                        Observaciones = "Deuda inicial"
                    });
                }

                db.Proveedores.Add(proveedor);
                await db.SaveChangesAsync(ct);

                var creado = await db.Proveedores
                    .AsNoTracking()
                    .Include(x => x.Movimientos)
                    .SingleAsync(x => x.Id == proveedor.Id, ct);

                return Ok(ProveedorDto.From(creado));
            }
            else
            {
                var proveedor = await db.Proveedores.SingleOrDefaultAsync(x => x.Id == request.Id, ct);
                if (proveedor is null)
                {
                    return BadRequest(new { errors = new[] { "El proveedor ya no existe." } });
                }

                proveedor.NombreRazonSocial = nombre;
                proveedor.Telefono = DisplayHelper.NormalizarOpcional(request.Telefono);
                proveedor.Email = DisplayHelper.NormalizarOpcional(request.Email);
                proveedor.Domicilio = DisplayHelper.NormalizarOpcional(request.Domicilio);
                proveedor.Observaciones = DisplayHelper.NormalizarOpcional(request.Observaciones);
                proveedor.Activo = true;
                await db.SaveChangesAsync(ct);

                var actualizado = await db.Proveedores
                    .AsNoTracking()
                    .Include(x => x.Movimientos)
                    .SingleAsync(x => x.Id == proveedor.Id, ct);

                return Ok(ProveedorDto.From(actualizado));
            }
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("{id:int}/movimientos")]
    public async Task<ActionResult<ProveedorDto>> RegistrarMovimiento(
        int id,
        [FromBody] MovimientoFormRequest request,
        CancellationToken ct)
    {
        try
        {
            RequireModulo(ModuloSistema.Proveedores);

            if (request.Monto <= 0)
            {
                return BadRequest(new { errors = new[] { "El monto debe ser mayor a cero." } });
            }

            await using var db = await dbContextFactory.CreateDbContextAsync(ct);
            var proveedor = await db.Proveedores
                .Include(x => x.Movimientos)
                .SingleOrDefaultAsync(x => x.Id == id, ct);

            if (proveedor is null)
            {
                return BadRequest(new { errors = new[] { "El proveedor ya no existe." } });
            }

            if (request.Tipo == TipoMovimientoProveedor.Pago)
            {
                var saldo = ProveedorHelper.CalcularSaldo(proveedor.Movimientos);
                if (request.Monto > saldo)
                {
                    return BadRequest(new { errors = new[] { $"El pago no puede superar el saldo pendiente ({MonedaHelper.FormatearUsd(saldo)})." } });
                }
            }

            db.MovimientosProveedor.Add(new MovimientoProveedor
            {
                ProveedorId = proveedor.Id,
                Tipo = request.Tipo,
                Fecha = request.Fecha.Date,
                Monto = request.Monto,
                Observaciones = DisplayHelper.NormalizarOpcional(request.Observaciones)
            });

            await db.SaveChangesAsync(ct);

            var actualizado = await db.Proveedores
                .AsNoTracking()
                .Include(x => x.Movimientos)
                .SingleAsync(x => x.Id == id, ct);

            return Ok(ProveedorDto.From(actualizado));
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
            RequireModulo(ModuloSistema.Proveedores);
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);
            var proveedor = await db.Proveedores.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (proveedor is null)
            {
                return NotFound(new { error = "El proveedor ya no existe." });
            }

            db.Proveedores.Remove(proveedor);
            await db.SaveChangesAsync(ct);
            return Ok();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    public sealed class ProveedorFormRequest
    {
        public int? Id { get; set; }
        public string NombreRazonSocial { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Domicilio { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public decimal DeudaInicial { get; set; }
    }

    public sealed class MovimientoFormRequest
    {
        public TipoMovimientoProveedor Tipo { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Today;
        public decimal Monto { get; set; }
        public string Observaciones { get; set; } = string.Empty;
    }

    public sealed record ProveedorDto(
        int Id,
        string? NombreRazonSocial,
        string? Telefono,
        string? Email,
        string? Domicilio,
        string? Observaciones,
        decimal Saldo,
        List<MovimientoDto> Movimientos)
    {
        public static ProveedorDto From(Proveedor p) => new(
            p.Id,
            p.NombreRazonSocial,
            p.Telefono,
            p.Email,
            p.Domicilio,
            p.Observaciones,
            ProveedorHelper.CalcularSaldo(p.Movimientos),
            p.Movimientos
                .OrderBy(x => x.Fecha)
                .ThenBy(x => x.Id)
                .Select(MovimientoDto.From)
                .ToList());
    }

    public sealed record MovimientoDto(
        int Id,
        DateTime Fecha,
        string Tipo,
        decimal Monto,
        string? Observaciones)
    {
        public static MovimientoDto From(MovimientoProveedor m) => new(
            m.Id,
            m.Fecha,
            m.Tipo.ToString(),
            m.Monto,
            m.Observaciones);
    }
}
