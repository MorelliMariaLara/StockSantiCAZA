namespace StockSantiCaza.Web.Helpers;

public static class DisplayHelper
{
    public static string Mostrar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? "-" : valor.Trim();

    public static string? NormalizarOpcional(string? valor)
    {
        var normalizado = valor?.Trim();
        return string.IsNullOrWhiteSpace(normalizado) ? null : normalizado;
    }

    public static string NormalizarTexto(string? valor) =>
        NormalizarOpcional(valor) ?? string.Empty;
}
