using System.Globalization;

namespace StockSantiCaza.Web;

public static class Formatos
{
    private static readonly CultureInfo UsdCulture = CultureInfo.GetCultureInfo("en-US");

    public static string MonedaUsd(decimal valor) => $"USD {valor.ToString("N2", UsdCulture)}";
}
