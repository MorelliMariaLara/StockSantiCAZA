namespace StockSantiCaza.Web.Services.Reportes;

public sealed record DashboardResumenDto(
    int CantidadVentas,
    decimal TotalVentas,
    int MovimientosStock,
    int ProductosConStockMinimo,
    IReadOnlyList<StockAlertaDto> AlertasStock);

public sealed record StockAlertaDto(
    string Sku,
    string Nombre,
    int StockActual,
    int StockMinimo);
