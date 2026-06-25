using System.Globalization;

namespace StockSantiCaza.Web.Helpers;

public static class MonedaHelper
{
  private static readonly CultureInfo CulturaUsd = CultureInfo.GetCultureInfo("en-US");

  public static string FormatearUsd(decimal monto) =>
      monto.ToString("C2", CulturaUsd);
}
