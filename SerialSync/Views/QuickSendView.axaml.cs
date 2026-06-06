using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SerialSync.ViewModels;

namespace SerialSync.Views;

public partial class QuickSendView : UserControl
{
    public QuickSendView()
    {
        InitializeComponent();
        ImportButton.Click += OnImportClick;
        ExportButton.Click += OnExportClick;
    }

    private QuickSendViewModel? Vm => DataContext as QuickSendViewModel;

    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is null)
            return;

        var provider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (provider is null)
            return;

        await Vm.ImportPresetsCommand.ExecuteAsync(provider);
    }

    private async void OnExportClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is null)
            return;

        var provider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (provider is null)
            return;

        await Vm.ExportPresetsCommand.ExecuteAsync(provider);
    }
}
