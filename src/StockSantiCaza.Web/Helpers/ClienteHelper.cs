using StockSantiCaza.Web.Models;

namespace StockSantiCaza.Web.Helpers;

public static class ClienteHelper
{
    public static bool EsDniInterno(string dniCuit) =>
        dniCuit.Length == 17
        && dniCuit.StartsWith('S')
        && dniCuit.Skip(1).All(char.IsDigit);

    public static string FormatearDocumentoCliente(Cliente cliente) =>
        EsDniInterno(cliente.DniCuit) ? "Sin documento" : cliente.DniCuit;
}
