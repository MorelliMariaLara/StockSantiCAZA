using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StockSantiCaza.Web.Data;

internal static class EfCoreConverters
{
    public static readonly ValueConverter<DateOnly?, DateTime?> NullableDateOnly = new(
        date => date.HasValue ? date.Value.ToDateTime(TimeOnly.MinValue) : null,
        dateTime => dateTime.HasValue ? DateOnly.FromDateTime(dateTime.Value) : null);
}
