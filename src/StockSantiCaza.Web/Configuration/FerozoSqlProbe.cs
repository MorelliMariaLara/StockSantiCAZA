using Microsoft.Data.SqlClient;

namespace StockSantiCaza.Web.Configuration;

public static class FerozoSqlProbe
{
    public sealed record Intento(string metodo, string dataSource, bool ok, int? sqlError, string? mensaje);

    private static readonly (string metodo, string dataSource, bool integrated)[] Estrategias =
    {
        ("sql2016,1433 + usuario SQL", "sql2016,1433", false),
        ("sql2016 + usuario SQL", "sql2016", false),
        ("tcp:sql2016,1433 + usuario SQL", "tcp:sql2016,1433", false),
        ("sql2016,1433 + Integrated Security", "sql2016,1433", true),
        ("sql2016 + Integrated Security", "sql2016", true),
    };

    public static async Task<IReadOnlyList<Intento>> ProbarTodasAsync(
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var personalizado = configuration["Database:DataSource"]?.Trim();
        if (!string.IsNullOrWhiteSpace(personalizado))
        {
            var builder = ConnectionStringResolver.CrearBuilder(configuration, personalizado);
            var unico = await ProbarUnoAsync("Database:DataSource configurado", builder, cancellationToken);
            return new[] { unico };
        }

        var resultados = new List<Intento>();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(25));

        foreach (var (metodo, dataSource, integrated) in Estrategias)
        {
            var builder = ConnectionStringResolver.CrearBuilder(configuration, dataSource, integrated);
            var intento = await ProbarUnoAsync(metodo, builder, timeout.Token);
            resultados.Add(intento);
            if (intento.ok)
            {
                break;
            }
        }

        return resultados;
    }

    public static async Task<(bool ok, string? dataSource, string? metodo)> EncontrarPrimeraAsync(
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var intentos = await ProbarTodasAsync(configuration, cancellationToken);
        var exitoso = intentos.FirstOrDefault(i => i.ok);
        return exitoso is null
            ? (false, null, null)
            : (true, exitoso.dataSource, exitoso.metodo);
    }

    private static async Task<Intento> ProbarUnoAsync(
        string metodo,
        SqlConnectionStringBuilder builder,
        CancellationToken cancellationToken)
    {
        builder.ConnectTimeout = 6;
        var dataSource = builder.DataSource;

        try
        {
            await using var conexion = new SqlConnection(builder.ConnectionString);
            await conexion.OpenAsync(cancellationToken);
            return new Intento(metodo, dataSource, true, null, null);
        }
        catch (OperationCanceledException)
        {
            return new Intento(metodo, dataSource, false, null, "Tiempo agotado");
        }
        catch (SqlException ex)
        {
            return new Intento(metodo, dataSource, false, ex.Number, ex.Message);
        }
        catch (Exception ex)
        {
            return new Intento(metodo, dataSource, false, null, ex.Message);
        }
    }
}
