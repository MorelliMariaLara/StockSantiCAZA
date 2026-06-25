using StockSantiCaza.Web.Models;

namespace StockSantiCaza.Web.Helpers;

public static class ProveedorHelper
{
    public static decimal CalcularSaldo(IEnumerable<MovimientoProveedor> movimientos) =>
        movimientos.Sum(m => m.Tipo == TipoMovimientoProveedor.Deuda ? m.Monto : -m.Monto);
}
