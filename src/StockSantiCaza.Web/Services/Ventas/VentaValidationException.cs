namespace StockSantiCaza.Web.Services.Ventas;

public class VentaValidationException : Exception
{
    public VentaValidationException(IEnumerable<string> errores)
        : base("La venta no cumple las validaciones requeridas.")
    {
        Errores = errores.ToArray();
    }

    public IReadOnlyList<string> Errores { get; }
}
