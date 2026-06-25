using System.ComponentModel.DataAnnotations;

namespace StockSantiCaza.Web.Models;

public class MovimientoStock
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public int? VentaId { get; set; }
    public Venta? Venta { get; set; }

    public int? ArmaId { get; set; }
    public Arma? Arma { get; set; }

    public int? MunicionLoteId { get; set; }
    public MunicionLote? MunicionLote { get; set; }

    public TipoMovimientoStock Tipo { get; set; }

    public int Cantidad { get; set; }

    public int StockResultante { get; set; }

    [MaxLength(500)]
    public string? Observacion { get; set; }
}
