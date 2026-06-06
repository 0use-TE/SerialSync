using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using SerialSync.Services;

namespace SerialSync.Views;

public partial class MainWindow : Window
{
    // Parameterless ctor required by Avalonia XAML loader; app uses DI ctor at runtime.
    public MainWindow()
    {
        InitializeComponent();
    }

    [ActivatorUtilitiesConstructor]
    public MainWindow(INotificationService notifications) : this()
    {
        _notifications = notifications;
        Opened += OnOpened;
    }

    private readonly INotificationService? _notifications;

    private void OnOpened(object? sender, EventArgs e) =>
        _notifications?.Initialize(this);
}
