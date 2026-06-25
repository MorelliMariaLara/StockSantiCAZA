using System.ComponentModel.DataAnnotations;

namespace StockSantiCaza.Web.Models;

public class Venta
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    public TipoComprobante TipoComprobante { get; set; }

    public EstadoVenta Estado { get; set; } = EstadoVenta.Confirmada;

    [MaxLength(40)]
    public string? PuntoVenta { get; set; }

    [MaxLength(40)]
    public string? NumeroComprobante { get; set; }

    [MaxLength(30)]
    public string? Cae { get; set; }

    public DateOnly? CaeVencimiento { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DescuentoTotal { get; set; }

    public decimal IvaTotal { get; set; }

    public decimal Total { get; set; }

    [MaxLength(1000)]
    public string? Observaciones { get; set; }

    public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
}
