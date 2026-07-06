using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Data;
using StockSantiCaza.Web.Models;

namespace StockSantiCaza.Web.Helpers;

public static class ClienteRapidoHelper
{
    public sealed record ClienteRapidoInput(
        string Nombre,
        string? DniCuit,
        string? Telefono,
        string? Email,
        string? Domicilio);

    public static async Task<Cliente> CrearOActualizarAsync(
        ApplicationDbContext db,
        ClienteRapidoInput input,
        CancellationToken ct = default)
    {
        var nombre = input.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new InvalidOperationException("Debe indicar el nombre del cliente.");
        }

        var telefono = DisplayHelper.NormalizarOpcional(input.Telefono);
        var email = DisplayHelper.NormalizarOpcional(input.Email);
        var domicilio = DisplayHelper.NormalizarOpcional(input.Domicilio);
        var dniCuit = DisplayHelper.NormalizarOpcional(input.DniCuit);

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
                return clienteExistente;
            }

            var cliente = CrearCliente(nombre, telefono, email, domicilio, dniCuit);
            db.Clientes.Add(cliente);
            await db.SaveChangesAsync(ct);
            return cliente;
        }

        var clienteNuevo = CrearCliente(nombre, telefono, email, domicilio, GenerarDniInterno());
        db.Clientes.Add(clienteNuevo);
        await db.SaveChangesAsync(ct);
        return clienteNuevo;
    }

    private static Cliente CrearCliente(
        string nombre,
        string? telefono,
        string? email,
        string? domicilio,
        string dniCuit) => new()
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
}
