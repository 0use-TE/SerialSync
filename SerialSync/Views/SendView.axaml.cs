using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SerialSync.Models;
using SerialSync.ViewModels;

namespace SerialSync.Views;

public partial class SendView : UserControl
{
    public SendView()
    {
        InitializeComponent();
        SendFileButton.Click += OnSendFileClick;
        HistoryList.DoubleTapped += OnHistoryDoubleTapped;
    }

    private SendViewModel? Vm => DataContext as SendViewModel;

    private async void OnSendFileClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is null)
            return;

        var provider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (provider is null)
            return;

        await Vm.SendFileCommand.ExecuteAsync(provider);
    }

    private async void OnHistoryDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (HistoryList.SelectedItem is SendRecord record && Vm?.ResendHistoryCommand.CanExecute(record) == true)
            await Vm.ResendHistoryCommand.ExecuteAsync(record);
    }
}
