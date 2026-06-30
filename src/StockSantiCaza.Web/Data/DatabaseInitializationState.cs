namespace StockSantiCaza.Web.Data;

public enum DatabaseInitStatus
{
    Pending,
    Initializing,
    Ready,
    Failed,
    Skipped
}

public sealed class DatabaseInitializationState
{
    private readonly object _lock = new();
    private DatabaseInitStatus _status = DatabaseInitStatus.Pending;
    private string? _errorMessage;

    public DatabaseInitStatus Status
    {
        get { lock (_lock) return _status; }
    }

    public string? ErrorMessage
    {
        get { lock (_lock) return _errorMessage; }
    }

    public bool IsReady
    {
        get
        {
            lock (_lock)
            {
                return _status is DatabaseInitStatus.Ready or DatabaseInitStatus.Skipped;
            }
        }
    }

    public void SetInitializing()
    {
        lock (_lock)
        {
            _status = DatabaseInitStatus.Initializing;
            _errorMessage = null;
        }
    }

    public void SetReady()
    {
        lock (_lock)
        {
            _status = DatabaseInitStatus.Ready;
            _errorMessage = null;
        }
    }

    public void SetSkipped()
    {
        lock (_lock)
        {
            _status = DatabaseInitStatus.Skipped;
            _errorMessage = null;
        }
    }

    public void SetFailed(string message)
    {
        lock (_lock)
        {
            _status = DatabaseInitStatus.Failed;
            _errorMessage = message;
        }
    }
}
