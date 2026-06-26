using System.ComponentModel.DataAnnotations;

namespace StockSantiCaza.Web.Models;

public class Proveedor
{
    public int Id { get; set; }

    [MaxLength(160)]
    public string? NombreRazonSocial { get; set; }

    [MaxLength(40)]
    public string? Telefono { get; set; }

    [MaxLength(180), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(220)]
    public string? Domicilio { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<MovimientoProveedor> Movimientos { get; set; } = new List<MovimientoProveedor>();
}
