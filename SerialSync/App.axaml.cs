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
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddDockInfrastructure();
        services.AddSingleton<LayoutStore>();
        services.AddSingleton<SendPresetStore>();
        services.AddSingleton<InMemoryLogSink>();
        services.AddSingleton<SendHistoryService>();
        services.AddSingleton<SerialTrafficService>();
        services.AddSingleton<SerialPortSettingsStore>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<ISerialPortService, SerialPortService>();

        services.AddDockPanel<SerialPortSettingsView, SerialPortSettingsViewModel>();
        services.AddDockPanel<ReceiveView, ReceiveViewModel>();
        services.AddDockPanel<SendView, SendViewModel>();
        services.AddDockPanel<QuickSendView, QuickSendViewModel>();
        services.AddDockPanel<SequenceView, SequenceViewModel>();
        services.AddDockPanel<LogView, LogViewModel>();

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainView>();
        services.AddMvvmSingleton<MainWindow, MainWindowViewModel>();
        services.AddMvvmSingleton<MainView, MainViewModel>();

        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddSerilog(dispose: true);
        });
    }

    public override void CreateShell(IServiceProvider serviceProvider)
    {
        DataTemplates.Add(serviceProvider.GetRequiredService<DockTabDataTemplate>());

        var layoutStore = serviceProvider.GetRequiredService<LayoutStore>();
        var memorySink = serviceProvider.GetRequiredService<InMemoryLogSink>();
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

        GlobalExceptionHandler.Install();

        CreateShellFromDi<MainWindow, MainView>(serviceProvider);
    }
}
