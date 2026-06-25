using System.ComponentModel.DataAnnotations;

namespace StockSantiCaza.Web.Models;

public class MovimientoProveedor
{
    public int Id { get; set; }

    public int ProveedorId { get; set; }

    public Proveedor Proveedor { get; set; } = null!;

    public TipoMovimientoProveedor Tipo { get; set; }

    public DateTime Fecha { get; set; }

    public decimal Monto { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }
}
