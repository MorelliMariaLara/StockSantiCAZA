namespace StockSantiCaza.Web.Services.Ui;

public sealed class ConfirmDialogService
{
    private TaskCompletionSource<bool>? _pending;

    public bool IsOpen { get; private set; }

    public string Title { get; private set; } = "Confirmar eliminación";

    public string Message { get; private set; } = string.Empty;

    public string ConfirmText { get; private set; } = "Borrar";

    public event Action? StateChanged;

    public Task<bool> AskAsync(
        string message,
        string title = "Confirmar eliminación",
        string confirmText = "Borrar")
    {
        if (IsOpen)
        {
            _pending?.TrySetResult(false);
        }

        Message = message;
        Title = title;
        ConfirmText = confirmText;
        IsOpen = true;
        _pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Notify();
        return _pending.Task;
    }

    public void Confirm()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        _pending?.TrySetResult(true);
        _pending = null;
        Notify();
    }

    public void Cancel()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        _pending?.TrySetResult(false);
        _pending = null;
        Notify();
    }

    private void Notify() => StateChanged?.Invoke();
}
