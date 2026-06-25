using System.ComponentModel.DataAnnotations;

namespace StockSantiCaza.Web.Services.Ventas;

public class NuevaVentaRequest
{
    public int? ClienteId { get; set; }

    public int? VendedorId { get; set; }

    [Range(0, 999999999)]
    public decimal DescuentoGeneral { get; set; }

    [MaxLength(1000)]
    public string? Observaciones { get; set; }

    [MinLength(1, ErrorMessage = "Debe agregar al menos un producto.")]
    public List<ItemVentaRequest> Items { get; set; } = [];
}

public class ItemVentaRequest
{
    public int ProductoId { get; set; }

    public int? ArmaId { get; set; }

    public int? MunicionLoteId { get; set; }

    [Range(1, int.MaxValue)]
    public int Cantidad { get; set; } = 1;

    [Range(0.01, 999999999)]
    public decimal PrecioUnitario { get; set; }

    [Range(0, 999999999)]
    public decimal Descuento { get; set; }
}

public sealed record VentaConfirmadaDto(
    int VentaId,
    string NumeroComprobante,
    decimal Total,
    EstadoVenta Estado,
    IReadOnlyList<string> Advertencias);
