namespace StockSantiCaza.Web.Helpers;

public static class FechaQueryHelper
{
    public static DateOnly? ParseOpcional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParse(value, out var fecha))
        {
            return fecha;
        }

        if (DateTime.TryParse(value, out var fechaHora))
        {
            return DateOnly.FromDateTime(fechaHora);
        }

        return null;
    }

    public static DateOnly ParseRequerida(string? value, string nombreParametro)
    {
        var fecha = ParseOpcional(value);
        if (!fecha.HasValue)
        {
            throw new InvalidOperationException(
                $"Parámetro '{nombreParametro}' inválido o faltante. Use formato yyyy-MM-dd.");
        }

        return fecha.Value;
    }
}
