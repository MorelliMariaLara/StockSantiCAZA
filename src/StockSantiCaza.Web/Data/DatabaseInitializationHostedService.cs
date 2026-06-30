namespace StockSantiCaza.Web.Data;

public sealed class DatabaseInitializationHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitializationHostedService> _logger;
    private readonly DatabaseInitializationState _state;

    public DatabaseInitializationHostedService(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger<DatabaseInitializationHostedService> logger,
        DatabaseInitializationState state)
    {
        _services = services;
        _configuration = configuration;
        _logger = logger;
        _state = state;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_configuration.GetValue<bool>("Database:SkipInitialization"))
        {
            _logger.LogInformation("[DbInitializer] Omitido en segundo plano (Database:SkipInitialization = true).");
            _state.SetSkipped();
            return;
        }

        try
        {
            _state.SetInitializing();
            _logger.LogInformation("[DbInitializer] Iniciando en segundo plano...");
            await DbInitializer.InitializeAsync(_services, _configuration, _logger, stoppingToken);
            _state.SetReady();
            _logger.LogInformation("[DbInitializer] Base de datos lista.");
        }
        catch (Exception ex)
        {
            _state.SetFailed(ex.Message);
            _logger.LogError(ex, "[DbInitializer] Error al inicializar la base de datos.");
        }
    }
}
