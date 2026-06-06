using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Crystal.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using SerialSync.Dock;
using SerialSync.Services;
using SerialSync.ViewModels;
using SerialSync.Views;

namespace SerialSync;

public partial class App : CrystalApplication
{
    private IServiceProvider? _serviceProvider;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddDockInfrastructure();
        services.AddSingleton<LayoutStore>();
        services.AddSingleton<SendPresetStore>();
        services.AddSingleton<SendHistoryStore>();
        services.AddSingleton<TextEncodingService>();
        services.AddSingleton<InMemoryLogSink>();
        services.AddSingleton<SendHistoryService>();
        services.AddSingleton<SerialTrafficService>();
        services.AddSingleton<SerialPortSettingsStore>();
        services.AddSingleton<INotificationService, NotificationService>();

#if BROWSER
        services.AddSingleton<IAppHostEnvironment, BrowserHostEnvironment>();
        services.AddSingleton<ISerialPortService, DemoSerialPortService>();
#else
        services.AddSingleton<IAppHostEnvironment, DesktopHostEnvironment>();
        services.AddSingleton<ISerialPortService, SerialPortService>();
#endif

        services.AddDockPanel<SerialPortSettingsView, SerialPortSettingsViewModel>();
        services.AddDockPanel<ReceiveView, ReceiveViewModel>();
        services.AddDockPanel<SendView, SendViewModel>();
        services.AddDockPanel<QuickSendView, QuickSendViewModel>();
        services.AddDockPanel<SequenceView, SequenceViewModel>();
        services.AddDockPanel<LogView, LogViewModel>();
        services.AddDockPanel<ToolsView, ToolsViewModel>();

#if !BROWSER
        services.AddSingleton<MainWindow>();
        services.AddMvvmSingleton<MainWindow, MainWindowViewModel>();
#endif
        services.AddSingleton<MainView>();
        services.AddMvvmSingleton<MainView, MainViewModel>();

        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddSerilog(dispose: true);
        });
    }

    public override void CreateShell(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        DataTemplates.Add(serviceProvider.GetRequiredService<DockTabDataTemplate>());

        var layoutStore = serviceProvider.GetRequiredService<LayoutStore>();
        var memorySink = serviceProvider.GetRequiredService<InMemoryLogSink>();

#if BROWSER
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Sink(memorySink)
            .WriteTo.Debug()
            .CreateLogger();
#else
        Directory.CreateDirectory(layoutStore.LogFolder);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(layoutStore.LogFolder, "log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .WriteTo.Sink(memorySink)
            .WriteTo.Debug()
            .CreateLogger();

        CreateShellFromDi<MainWindow, MainView>(serviceProvider);
#endif

        GlobalExceptionHandler.Install();
    }

    public override void OnFrameworkInitializationCompleted()
    {
#if BROWSER
        if (_serviceProvider is not null &&
            ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            var mainView = _serviceProvider.GetRequiredService<MainView>();
            var notifications = _serviceProvider.GetRequiredService<INotificationService>();

            mainView.AttachedToVisualTree += (_, _) =>
            {
                if (TopLevel.GetTopLevel(mainView) is { } top)
                    notifications.Initialize(top);
            };

            singleView.MainView = mainView;
        }
#endif

        base.OnFrameworkInitializationCompleted();
    }
}
