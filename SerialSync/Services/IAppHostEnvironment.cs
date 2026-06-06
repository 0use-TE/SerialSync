namespace SerialSync.Services;

public interface IAppHostEnvironment
{
    bool IsBrowserPreview { get; }
}

public sealed class DesktopHostEnvironment : IAppHostEnvironment
{
    public bool IsBrowserPreview => false;
}

public sealed class BrowserHostEnvironment : IAppHostEnvironment
{
    public bool IsBrowserPreview => true;
}
