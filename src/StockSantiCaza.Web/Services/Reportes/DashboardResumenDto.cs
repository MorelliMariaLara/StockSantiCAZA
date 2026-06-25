namespace StockSantiCaza.Web.Services.Reportes;

public sealed record DashboardResumenDto(
    int CantidadVentas,
    decimal TotalVentas,
    decimal GananciaTotal,
    int MovimientosStock,
    int ProductosConStockMinimo,
    IReadOnlyList<StockAlertaDto> AlertasStock);

public sealed record StockAlertaDto(
    string Sku,
    string Nombre,
    int StockActual,
    int StockMinimo);

public sealed record ReportePeriodoDto(
    int CantidadVentas,
    decimal TotalVentas,
    decimal GananciaTotal,
    int MovimientosStock,
    int ProductosConStockMinimo,
    IReadOnlyList<StockAlertaDto> AlertasStock);
