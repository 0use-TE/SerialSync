namespace SerialSync.Services;

public interface INotificationService
{
    void Initialize(Avalonia.Controls.TopLevel topLevel);

    void ShowSuccess(string title, string? message = null);

    void ShowError(string title, string? message = null);

    void ShowWarning(string title, string? message = null);

    void ShowInfo(string title, string? message = null);
}
