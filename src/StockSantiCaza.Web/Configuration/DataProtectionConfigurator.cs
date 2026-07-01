using Microsoft.AspNetCore.DataProtection;

namespace StockSantiCaza.Web.Configuration;

public static class DataProtectionConfigurator
{
    public static void Configure(WebApplicationBuilder builder)
    {
        var keysPath = Path.Combine(builder.Environment.ContentRootPath, "keys");
        var dataProtection = builder.Services.AddDataProtection()
            .SetApplicationName("StockSantiCaza.Web");

        try
        {
            Directory.CreateDirectory(keysPath);
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
            Console.WriteLine($"[StockSantiCAZA] DataProtection: {keysPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[StockSantiCAZA] ADVERTENCIA: no se pudo usar la carpeta keys ({ex.Message}). " +
                "La app arranca igual; las sesiones se pierden al reiniciar el App Pool.");
        }
    }
}
