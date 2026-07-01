using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Data;
using StockSantiCaza.Web.Helpers;
using StockSantiCaza.Web.Models;
using StockSantiCaza.Web.Services.Auth;

namespace StockSantiCaza.Web.Controllers.Api;

[ApiController]
[Route("api/clientes")]
public class ClientesController : ApiControllerBase
{
    private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;

    public ClientesController(IAuthService authService, IDbContextFactory<ApplicationDbContext> dbContextFactory)
        : base(authService)
    {
        this.dbContextFactory = dbContextFactory;
    }

    [HttpGet]
    public async Task<ActionResult<List<ClienteDto>>> Listar(CancellationToken ct)
    {
        try
        {
            RequireModulo(ModuloSistema.Clientes);
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);
            var clientes = await ListarClientesDtoAsync(db, ct);

            return Ok(clientes);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult<ClienteDto>> Guardar([FromBody] ClienteFormRequest request, CancellationToken ct)
    {
        try
        {
            RequireModulo(ModuloSistema.Clientes);
            var errores = new List<string>();

            var nombre = DisplayHelper.NormalizarTexto(request.NombreRazonSocial);
            var dniCuit = DisplayHelper.NormalizarOpcional(request.DniCuit);
            var numeroLegajo = DisplayHelper.NormalizarOpcional(request.NumeroLegajoClu);

            await using var db = await dbContextFactory.CreateDbContextAsync(ct);

            if (!string.IsNullOrWhiteSpace(dniCuit))
            {
                var dniDuplicado = await db.Clientes.AnyAsync(x =>
                    x.DniCuit == dniCuit && (!request.Id.HasValue || x.Id != request.Id.Value), ct);
                if (dniDuplicado)
                {
                    errores.Add($"Ya existe un cliente con DNI/CUIT {dniCuit}.");
                }
            }

            if (request.TieneClu && !string.IsNullOrWhiteSpace(numeroLegajo))
            {
                var cluDuplicada = await db.CredencialesCLU.AnyAsync(x =>
                    x.NumeroLegajo == numeroLegajo
                    && (!request.Id.HasValue || x.ClienteId != request.Id.Value), ct);
                if (cluDuplicada)
                {
                    errores.Add($"Ya existe una CLU con legajo {numeroLegajo}.");
                }
            }

            if (errores.Count > 0)
            {
                return BadRequest(new { errors = errores });
            }

            Cliente cliente;
            var esNuevo = request.Id is null;

            if (esNuevo)
            {
                cliente = new Cliente();
                db.Clientes.Add(cliente);
            }
            else
            {
                var clienteExistente = await db.Clientes
                    .Include(x => x.CredencialCLU)
                    .SingleOrDefaultAsync(x => x.Id == request.Id, ct);
                if (clienteExistente is null)
                {
                    return BadRequest(new { errors = new[] { "El cliente seleccionado ya no existe." } });
                }

                cliente = clienteExistente;
            }

            cliente.NombreRazonSocial = nombre;
            cliente.DniCuit = dniCuit;
            cliente.Email = DisplayHelper.NormalizarOpcional(request.Email);
            cliente.Telefono = DisplayHelper.NormalizarOpcional(request.Telefono);
            cliente.Domicilio = DisplayHelper.NormalizarOpcional(request.Domicilio);
            cliente.Activo = true;

            if (request.TieneClu && !string.IsNullOrWhiteSpace(numeroLegajo))
            {
                cliente.CredencialCLU ??= new CredencialCLU();
                cliente.CredencialCLU.NumeroLegajo = numeroLegajo;
                cliente.CredencialCLU.FechaEmision = request.FechaEmisionClu;
                cliente.CredencialCLU.FechaVencimiento = request.FechaVencimientoClu;
            }
            else if (cliente.CredencialCLU is not null)
            {
                db.CredencialesCLU.Remove(cliente.CredencialCLU);
                cliente.CredencialCLU = null;
            }

            await db.SaveChangesAsync(ct);

            var guardado = await ObtenerClienteDtoAsync(db, cliente.Id, ct);

            return Ok(guardado);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("rapido")]
    public async Task<ActionResult<ClienteDto>> CrearRapido([FromBody] ClienteRapidoRequest request, CancellationToken ct)
    {
        try
        {
            RequireModulo(ModuloSistema.Clientes);
            var nombre = request.Nombre.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return BadRequest(new { errors = new[] { "Debe indicar el nombre del cliente." } });
            }

            var telefono = DisplayHelper.NormalizarOpcional(request.Telefono);
            var email = DisplayHelper.NormalizarOpcional(request.Email);
            var domicilio = DisplayHelper.NormalizarOpcional(request.Domicilio);
            var dniCuit = request.DniCuit.Trim();

            await using var db = await dbContextFactory.CreateDbContextAsync(ct);
            Cliente cliente;

            if (!string.IsNullOrWhiteSpace(dniCuit))
            {
                var clienteExistente = await db.Clientes
                    .Include(x => x.CredencialCLU)
                    .SingleOrDefaultAsync(x => x.DniCuit == dniCuit, ct);

                if (clienteExistente is not null)
                {
                    clienteExistente.NombreRazonSocial = nombre;
                    clienteExistente.Telefono = telefono ?? clienteExistente.Telefono;
                    clienteExistente.Email = email ?? clienteExistente.Email;
                    clienteExistente.Domicilio = domicilio ?? clienteExistente.Domicilio;
                    clienteExistente.Activo = true;
                    await db.SaveChangesAsync(ct);
                    cliente = clienteExistente;
                }
                else
                {
                    cliente = CrearCliente(nombre, telefono, email, domicilio, dniCuit);
                    db.Clientes.Add(cliente);
                    await db.SaveChangesAsync(ct);
                }
            }
            else
            {
                cliente = CrearCliente(nombre, telefono, email, domicilio, GenerarDniInterno());
                db.Clientes.Add(cliente);
                await db.SaveChangesAsync(ct);
            }

            var guardado = await ObtenerClienteDtoAsync(db, cliente.Id, ct);

            return Ok(guardado);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    private static Task<List<ClienteDto>> ListarClientesDtoAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        return db.Clientes
            .AsNoTracking()
            .OrderBy(c => c.NombreRazonSocial)
            .Select(c => new ClienteDto(
                c.Id,
                c.NombreRazonSocial,
                c.DniCuit,
                c.Email,
                c.Telefono,
                c.Domicilio,
                c.CredencialCLU == null
                    ? null
                    : new CredencialCluDto(
                        c.CredencialCLU.NumeroLegajo ?? string.Empty,
                        c.CredencialCLU.FechaEmision,
                        c.CredencialCLU.FechaVencimiento,
                        c.CredencialCLU.FechaVencimiento.HasValue
                            && c.CredencialCLU.FechaVencimiento.Value >= hoy),
                c.Ventas.Count,
                c.ArmasRegistradas.Count))
            .ToListAsync(ct);
    }

    private static Task<ClienteDto> ObtenerClienteDtoAsync(ApplicationDbContext db, int id, CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        return db.Clientes
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ClienteDto(
                c.Id,
                c.NombreRazonSocial,
                c.DniCuit,
                c.Email,
                c.Telefono,
                c.Domicilio,
                c.CredencialCLU == null
                    ? null
                    : new CredencialCluDto(
                        c.CredencialCLU.NumeroLegajo ?? string.Empty,
                        c.CredencialCLU.FechaEmision,
                        c.CredencialCLU.FechaVencimiento,
                        c.CredencialCLU.FechaVencimiento.HasValue
                            && c.CredencialCLU.FechaVencimiento.Value >= hoy),
                c.Ventas.Count,
                c.ArmasRegistradas.Count))
            .SingleAsync(ct);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        try
        {
            RequireModulo(ModuloSistema.Clientes);
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);

            var tieneVentas = await db.Ventas.AnyAsync(x => x.ClienteId == id, ct);
            if (tieneVentas)
            {
                return BadRequest(new { error = "No se puede eliminar el cliente porque tiene ventas registradas." });
            }

            var cliente = await db.Clientes
                .Include(x => x.CredencialCLU)
                .SingleOrDefaultAsync(x => x.Id == id, ct);

            if (cliente is null)
            {
                return NotFound(new { error = "El cliente seleccionado ya no existe." });
            }

            db.Clientes.Remove(cliente);
            await db.SaveChangesAsync(ct);
            return Ok();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    private static Cliente CrearCliente(string nombre, string? telefono, string? email, string? domicilio, string dniCuit) => new()
    {
        NombreRazonSocial = nombre,
        DniCuit = dniCuit,
        Telefono = telefono,
        Email = email,
        Domicilio = domicilio,
        Activo = true
    };

    private static string GenerarDniInterno() =>
        $"S{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";

    public sealed class ClienteFormRequest
    {
        public int? Id { get; set; }
        public string NombreRazonSocial { get; set; } = string.Empty;
        public string DniCuit { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Domicilio { get; set; } = string.Empty;
        public bool TieneClu { get; set; }
        public string NumeroLegajoClu { get; set; } = string.Empty;
        public DateOnly? FechaEmisionClu { get; set; }
        public DateOnly? FechaVencimientoClu { get; set; }
    }

    public sealed class ClienteRapidoRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string DniCuit { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Domicilio { get; set; } = string.Empty;
    }

    public sealed record ClienteDto(
        int Id,
        string? NombreRazonSocial,
        string? DniCuit,
        string? Email,
        string? Telefono,
        string? Domicilio,
        CredencialCluDto? CredencialClu,
        int CantidadVentas,
        int CantidadArmas)
    {
        public static ClienteDto From(Cliente c) => new(
            c.Id,
            c.NombreRazonSocial,
            c.DniCuit,
            c.Email,
            c.Telefono,
            c.Domicilio,
            c.CredencialCLU is null ? null : CredencialCluDto.From(c.CredencialCLU),
            c.Ventas.Count,
            c.ArmasRegistradas.Count);
    }

    public sealed record CredencialCluDto(
        string NumeroLegajo,
        DateOnly? FechaEmision,
        DateOnly? FechaVencimiento,
        bool EstaVigente)
    {
        public static CredencialCluDto From(CredencialCLU clu) => new(
            clu.NumeroLegajo,
            clu.FechaEmision,
            clu.FechaVencimiento,
            clu.EstaVigente);
    }
}
