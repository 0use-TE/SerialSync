using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SerialSync.Views;

public partial class TutorialView : UserControl
{
    public TutorialView()
    {
        InitializeComponent();
        GitHubLink.Click += OnGitHubLinkClick;
        WebPreviewLink.Click += OnWebPreviewLinkClick;
    }

    private async void OnWebPreviewLinkClick(object? sender, RoutedEventArgs e)
    {
        var launcher = TopLevel.GetTopLevel(this)?.Launcher;
        if (launcher is null)
            return;

        await launcher.LaunchUriAsync(new Uri(AppConstants.WebPreviewUrl));
    }

    private async void OnGitHubLinkClick(object? sender, RoutedEventArgs e)
    {
        var launcher = TopLevel.GetTopLevel(this)?.Launcher;
        if (launcher is null)
            return;

        await launcher.LaunchUriAsync(new Uri(AppConstants.GitHubRepositoryUrl));
    }
}
