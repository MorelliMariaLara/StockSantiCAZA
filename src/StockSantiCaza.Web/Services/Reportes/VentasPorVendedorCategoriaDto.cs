namespace StockSantiCaza.Web.Services.Reportes;

public sealed record VentasPorVendedorCategoriaDto(
    string Desde,
    string Hasta,
    IReadOnlyList<VendedorCategoriaResumenDto> Vendedores,
    IReadOnlyList<CategoriaTotalDto> TotalesPorCategoria,
    int CantidadUnidades,
    decimal MontoTotal);

public sealed record VendedorCategoriaResumenDto(
    int? VendedorId,
    string Vendedor,
    IReadOnlyList<CategoriaTotalDto> Categorias,
    int CantidadUnidades,
    decimal MontoTotal);

public sealed record CategoriaTotalDto(
    string Categoria,
    int Cantidad,
    decimal Monto);
