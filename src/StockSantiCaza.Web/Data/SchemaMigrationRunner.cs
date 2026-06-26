using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace StockSantiCaza.Web.Data;

public static class SchemaMigrationRunner
{
    private const string BatchSeparator = "--BATCH";
    private const string ResourceName = "StockSantiCaza.Web.Data.schema-migration.sql";

    public static async Task ApplyAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        var script = LoadScript();
        var batches = script.Split(BatchSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var batch in batches)
        {
            var sql = batch.Trim();
            if (string.IsNullOrWhiteSpace(sql))
            {
                continue;
            }

            await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
    }

    private static string LoadScript()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"No se encontró el script de migración embebido: {ResourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
