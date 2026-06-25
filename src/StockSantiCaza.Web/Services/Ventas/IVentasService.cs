namespace StockSantiCaza.Web.Services.Ventas;

public interface IVentasService
{
    Task<VentaConfirmadaDto> ConfirmarVentaAsync(NuevaVentaRequest request, CancellationToken cancellationToken = default);
}
