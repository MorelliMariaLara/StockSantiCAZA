using StockSantiCaza.Web.Models;

namespace StockSantiCaza.Web.Helpers;

public static class FacturaHelper
{
    public static bool EsDniInterno(string dniCuit) =>
        dniCuit.Length == 17
        && dniCuit.StartsWith('S')
        && dniCuit.Skip(1).All(char.IsDigit);

    public static string ObtenerLetra(TipoComprobante tipo) => tipo switch
    {
        TipoComprobante.FacturaA => "A",
        TipoComprobante.FacturaB => "B",
        TipoComprobante.FacturaC => "C",
        _ => "X"
    };

    public static string ObtenerCodigoAfip(TipoComprobante tipo) => tipo switch
    {
        TipoComprobante.FacturaA => "001",
        TipoComprobante.FacturaB => "006",
        TipoComprobante.FacturaC => "011",
        _ => "000"
    };

    public static string FormatearNumeroComprobante(string puntoVenta, int ventaId) =>
        $"{puntoVenta.PadLeft(4, '0')}-{ventaId:D8}";

    public static string ObtenerCondicionCliente(Cliente cliente, TipoComprobante tipo) =>
        tipo switch
        {
            TipoComprobante.FacturaA => "IVA Responsable Inscripto",
            TipoComprobante.FacturaB => "Consumidor Final",
            TipoComprobante.FacturaC => "Consumidor Final",
            _ => "—"
        };

    public static string FormatearDocumentoCliente(Cliente cliente) =>
        EsDniInterno(cliente.DniCuit) ? "Sin documento" : cliente.DniCuit;

    public static bool EsFactura(TipoComprobante tipo) =>
        tipo is TipoComprobante.FacturaA or TipoComprobante.FacturaB or TipoComprobante.FacturaC;
}
