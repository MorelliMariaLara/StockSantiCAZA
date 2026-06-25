namespace StockSantiCaza.Web.Services.Reportes;

public interface IReportesService
{
    Task<DashboardResumenDto> ObtenerDashboardAsync(DateOnly fecha, CancellationToken cancellationToken = default);

    Task<byte[]> ExportarVentasExcelAsync(DateOnly desde, DateOnly hasta, CancellationToken cancellationToken = default);
}
