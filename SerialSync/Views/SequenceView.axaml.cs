using Avalonia.Controls;
using Avalonia.Interactivity;
using SerialSync.ViewModels;

namespace SerialSync.Views;

public partial class SequenceView : UserControl
{
    public SequenceView() => InitializeComponent();

    private void OnSequenceNameLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SequenceViewModel vm)
            vm.CommitSequenceNameCommand.Execute(null);
    }
}
