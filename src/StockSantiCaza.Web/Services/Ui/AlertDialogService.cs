namespace StockSantiCaza.Web.Services.Ui;

public enum AlertKind
{
    Error,
    Warning,
    Success,
    Info
}

public sealed class AlertDialogService
{
    private TaskCompletionSource? _pending;

    public bool IsOpen { get; private set; }

    public string Title { get; private set; } = "Atención";

    public string Message { get; private set; } = string.Empty;

    public AlertKind Kind { get; private set; } = AlertKind.Error;

    public event Action? StateChanged;

    public Task ShowAsync(
        string message,
        string title = "Atención",
        AlertKind kind = AlertKind.Error)
    {
        if (IsOpen)
        {
            _pending?.TrySetResult();
        }

        Message = message;
        Title = title;
        Kind = kind;
        IsOpen = true;
        _pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Notify();
        return _pending.Task;
    }

    public Task ShowErrorAsync(string message, string title = "No se pudo completar la acción") =>
        ShowAsync(message, title, AlertKind.Error);

    public Task ShowWarningAsync(string message, string title = "Advertencia") =>
        ShowAsync(message, title, AlertKind.Warning);

    public Task ShowSuccessAsync(string message, string title = "Operación exitosa") =>
        ShowAsync(message, title, AlertKind.Success);

    public Task ShowErrorsAsync(IEnumerable<string> messages, string title = "No se pudo completar la acción")
    {
        var lista = messages.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var texto = lista.Count switch
        {
            0 => "Ocurrió un error desconocido.",
            1 => lista[0],
            _ => string.Join(Environment.NewLine, lista.Select(x => $"• {x}"))
        };

        return ShowAsync(texto, title, AlertKind.Error);
    }

    public void Dismiss()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        _pending?.TrySetResult();
        _pending = null;
        Notify();
    }

    private void Notify() => StateChanged?.Invoke();
}
