namespace StockSantiCaza.Web.Configuration;

public class EmpresaFiscalOptions
{
    public const string SectionName = "EmpresaFiscal";

    public string RazonSocial { get; set; } = "StockSantiCAZA S.R.L.";

    public string NombreFantasia { get; set; } = "StockSantiCAZA - Armería";

    public string Cuit { get; set; } = "30-00000000-0";

    public string DomicilioComercial { get; set; } = "Argentina";

    public string CondicionIva { get; set; } = "IVA Responsable Inscripto";

    public string IngresosBrutos { get; set; } = "Exento";

    public string InicioActividades { get; set; } = "01/01/2020";

    public string PuntoVenta { get; set; } = "0001";
}
