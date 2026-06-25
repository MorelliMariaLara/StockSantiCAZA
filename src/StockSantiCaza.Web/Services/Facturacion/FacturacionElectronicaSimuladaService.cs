using StockSantiCaza.Web.Models;

namespace StockSantiCaza.Web.Services.Facturacion;

public class FacturacionElectronicaSimuladaService : IFacturacionElectronicaService
{
    public Task<ComprobanteFiscalDto> EmitirAsync(Venta venta, CancellationToken cancellationToken = default)
    {
        var puntoVenta = "0001";
        var tipo = venta.TipoComprobante switch
        {
            TipoComprobante.FacturaA => "A",
            TipoComprobante.FacturaB => "B",
            TipoComprobante.FacturaC => "C",
            _ => "P"
        };

        var numero = venta.TipoComprobante == TipoComprobante.Presupuesto
            ? $"PRES-{DateTime.UtcNow:yyyyMMddHHmmss}"
            : $"{tipo}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var cae = venta.TipoComprobante == TipoComprobante.Presupuesto
            ? null
            : Random.Shared.NextInt64(10000000000000, 99999999999999).ToString();

        DateOnly? vencimiento = cae is null ? null : DateOnly.FromDateTime(DateTime.Today.AddDays(10));

        return Task.FromResult(new ComprobanteFiscalDto(puntoVenta, numero, cae, vencimiento));
    }
}
