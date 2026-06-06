using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SerialSync.ViewModels;
using System.ComponentModel;

namespace SerialSync.Views;

public partial class ReceiveView : UserControl
{
    private ReceiveViewModel? _vm;

    public ReceiveView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        SaveReceiveButton.Click += OnSaveReceiveClick;
    }

    private async void OnSaveReceiveClick(object? sender, RoutedEventArgs e)
    {
        if (_vm is null)
            return;

        var provider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (provider is null)
            return;

        await _vm.SaveToFileCommand.ExecuteAsync(provider);
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (_vm is not null)
            _vm.PropertyChanged -= OnVmPropertyChanged;

        _vm = DataContext as ReceiveViewModel;
        if (_vm is not null)
            _vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ReceiveViewModel.DisplayText) || _vm is null || !_vm.AutoScroll)
            return;

        ReceiveScroll.ScrollToEnd();
    }
}
