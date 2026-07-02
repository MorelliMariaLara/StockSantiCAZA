namespace StockSantiCaza.Web.Configuration;

public sealed class FerozoConnectionStringProvider
{
    private readonly IConfiguration configuration;
    private readonly IHostEnvironment environment;
    private readonly ILogger<FerozoConnectionStringProvider> logger;
    private readonly SemaphoreSlim semaforo = new(1, 1);
    private string? cadenaCache;

    public FerozoConnectionStringProvider(
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<FerozoConnectionStringProvider> logger)
    {
        this.configuration = configuration;
        this.environment = environment;
        this.logger = logger;
    }

    public string ObtenerCadena() =>
        cadenaCache ?? ConnectionStringResolver.Resolve(configuration);

    public async Task<string> ObtenerCadenaAsync(CancellationToken cancellationToken = default)
    {
        if (cadenaCache is not null)
        {
            return cadenaCache;
        }

        if (!string.IsNullOrWhiteSpace(configuration["Database:DataSource"]))
        {
            cadenaCache = ConnectionStringResolver.Resolve(configuration);
            return cadenaCache;
        }

        if (environment.IsDevelopment())
        {
            cadenaCache = ConnectionStringResolver.Resolve(configuration);
            return cadenaCache;
        }

        await semaforo.WaitAsync(cancellationToken);
        try
        {
            if (cadenaCache is not null)
            {
                return cadenaCache;
            }

            var (ok, dataSource, metodo) = await FerozoSqlProbe.EncontrarPrimeraAsync(configuration, cancellationToken);
            if (ok && dataSource is not null)
            {
                cadenaCache = ConnectionStringResolver.Resolve(configuration, dataSource);
                logger.LogInformation("[SQL] Conexión Ferozo: {Metodo} ({DataSource})", metodo, dataSource);
                return cadenaCache;
            }

            logger.LogWarning("[SQL] Ninguna estrategia Ferozo conectó; se usa cadena por defecto.");
            cadenaCache = ConnectionStringResolver.Resolve(configuration);
            return cadenaCache;
        }
        finally
        {
            semaforo.Release();
        }
    }
}
