using Avalonia.Controls;
using Avalonia.Controls.Notifications;

namespace SerialSync.Services;

public sealed class NotificationService : INotificationService
{
    private WindowNotificationManager? _manager;

    public void Initialize(TopLevel topLevel)
    {
        _manager = new WindowNotificationManager(topLevel)
        {
            Position = NotificationPosition.BottomRight,
            MaxItems = 5,
        };
    }

    public void ShowSuccess(string title, string? message = null) =>
        Show(title, message, NotificationType.Success);

    public void ShowError(string title, string? message = null) =>
        Show(title, message, NotificationType.Error);

    public void ShowWarning(string title, string? message = null) =>
        Show(title, message, NotificationType.Warning);

    public void ShowInfo(string title, string? message = null) =>
        Show(title, message, NotificationType.Information);

    private void Show(string title, string? message, NotificationType type)
    {
        if (_manager is null)
            return;

        _manager.Show(new Notification(title, message ?? string.Empty, type, TimeSpan.FromSeconds(3.5)));
    }
}
