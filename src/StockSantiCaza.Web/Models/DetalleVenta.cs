namespace StockSantiCaza.Web.Models;

public class DetalleVenta
{
    public int Id { get; set; }

    public int VentaId { get; set; }
    public Venta Venta { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public int? ArmaId { get; set; }
    public Arma? Arma { get; set; }

    public int? MunicionLoteId { get; set; }
    public MunicionLote? MunicionLote { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Descuento { get; set; }

    public decimal AlicuotaIva { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Iva { get; set; }

    public decimal Total { get; set; }
}
