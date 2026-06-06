using Avalonia;
using Avalonia.Browser;
using SerialSync;

internal static class Program
{
    private static Task Main(string[] args) => BuildAvaloniaApp()
        .WithInterFont()
        .StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UseBrowser()
            .LogToTrace();
}
