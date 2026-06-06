using Avalonia.Controls;
using SerialSync.ViewModels;

namespace SerialSync.Views;

public partial class MainView : UserControl
{
    public MainView() => InitializeComponent();

    protected override async void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is MainViewModel mainVm)
            await mainVm.OnLoadedAsync();
    }

    protected override async void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            await vm.OnUnloaded();

        base.OnUnloaded(e);
    }
}
