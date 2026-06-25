using Microsoft.Extensions.Options;
using StockSantiCaza.Web.Configuration;
using StockSantiCaza.Web.Helpers;
using StockSantiCaza.Web.Models;

namespace StockSantiCaza.Web.Services.Facturacion;

public class FacturacionElectronicaSimuladaService(IOptions<EmpresaFiscalOptions> empresaOptions)
    : IFacturacionElectronicaService
{
    public Task<ComprobanteFiscalDto> EmitirAsync(Venta venta, CancellationToken cancellationToken = default)
    {
        var empresa = empresaOptions.Value;
        var puntoVenta = empresa.PuntoVenta.PadLeft(4, '0');

        if (!FacturaHelper.EsFactura(venta.TipoComprobante))
        {
            var presupuesto = $"PRES-{venta.Id:D8}";
            return Task.FromResult(new ComprobanteFiscalDto(
                puntoVenta,
                presupuesto,
                null,
                null,
                venta.TipoComprobante,
                false));
        }

        var numero = FacturaHelper.FormatearNumeroComprobante(puntoVenta, venta.Id);
        var cae = Random.Shared.NextInt64(10000000000000, 99999999999999).ToString();
        var vencimiento = DateOnly.FromDateTime(DateTime.Today.AddDays(10));

        return Task.FromResult(new ComprobanteFiscalDto(
            puntoVenta,
            numero,
            cae,
            vencimiento,
            venta.TipoComprobante,
            true));
    }
}
