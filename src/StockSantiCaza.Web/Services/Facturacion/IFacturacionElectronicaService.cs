using StockSantiCaza.Web.Models;

namespace StockSantiCaza.Web.Services.Facturacion;

public interface IFacturacionElectronicaService
{
    Task<ComprobanteFiscalDto> EmitirAsync(Venta venta, CancellationToken cancellationToken = default);
}

public sealed record ComprobanteFiscalDto(
    string PuntoVenta,
    string NumeroComprobante,
    string? Cae,
    DateOnly? CaeVencimiento,
    TipoComprobante TipoComprobante,
    bool EsFactura);
