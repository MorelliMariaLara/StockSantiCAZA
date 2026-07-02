using Microsoft.Data.SqlClient;

namespace StockSantiCaza.Web.Configuration;

public static class FerozoSqlProbe
{
    public sealed record Metodo(int id, string nombre, string dataSource, bool integrated);

    public sealed record Resultado(int id, string nombre, string dataSource, bool ok, int? sqlError, string? mensaje);

    /// <summary>
    /// DonWeb: Server=sql2016. ASP.NET Core necesita puerto 1433 para TCP (evita Named Pipes error 53).
    /// sql2016,1433 sigue siendo el servidor sql2016 — no es IP:2082.
    /// </summary>
    public static readonly Metodo[] Metodos =
    {
        new(1, "sql2016,1433 + usuario SQL (TCP, recomendado .NET Core)", "sql2016,1433", false),
        new(2, "sql2016 + usuario SQL (Named Pipes)", "sql2016", false),
        new(3, "sql2016 + Integrated Security (panel SSPI)", "sql2016", true),
    };

    public static async Task<Resultado> ProbarMetodoAsync(
        IConfiguration configuration,
        int id,
        CancellationToken cancellationToken = default)
    {
        var metodo = Metodos.FirstOrDefault(m => m.id == id)
            ?? throw new ArgumentOutOfRangeException(nameof(id), "Usá metodo=1 o metodo=2");

        var builder = ConnectionStringResolver.CrearBuilder(configuration, metodo.dataSource, metodo.integrated);
        builder.ConnectTimeout = 8;

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            await using var conexion = new SqlConnection(builder.ConnectionString);
            await conexion.OpenAsync(timeout.Token);
            return new Resultado(metodo.id, metodo.nombre, metodo.dataSource, true, null, null);
        }
        catch (OperationCanceledException)
        {
            return new Resultado(metodo.id, metodo.nombre, metodo.dataSource, false, null, "Tiempo agotado");
        }
        catch (SqlException ex)
        {
            return new Resultado(metodo.id, metodo.nombre, metodo.dataSource, false, ex.Number, ex.Message);
        }
        catch (Exception ex)
        {
            return new Resultado(metodo.id, metodo.nombre, metodo.dataSource, false, null, ex.Message);
        }
    }
}
