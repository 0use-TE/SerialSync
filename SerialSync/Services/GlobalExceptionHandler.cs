using Avalonia.Threading;
using Serilog;

namespace SerialSync.Services;

public static class GlobalExceptionHandler
{
    private static bool _installed;

    public static void Install()
    {
        if (_installed)
            return;

        _installed = true;

        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandled;
        TaskScheduler.UnobservedTaskException += OnUnobservedTask;

        if (Dispatcher.UIThread.CheckAccess())
            HookUiDispatcher();
        else
            Dispatcher.UIThread.Post(HookUiDispatcher);
    }

    private static void HookUiDispatcher() =>
        Dispatcher.UIThread.UnhandledException += OnUiUnhandled;

    private static void OnAppDomainUnhandled(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        WriteLog(ex ?? new Exception(e.ExceptionObject?.ToString() ?? "未知致命错误"), "致命未捕获异常", fatal: true);
    }

    private static void OnUnobservedTask(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        foreach (var inner in e.Exception.Flatten().InnerExceptions)
            WriteLog(inner, "未观察到的 Task 异常", fatal: false);

        e.SetObserved();
    }

    private static void OnUiUnhandled(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteLog(e.Exception, "UI 未处理异常", fatal: false);
        e.Handled = true;
    }

    private static void WriteLog(Exception ex, string caption, bool fatal)
    {
        if (Log.Logger is not null && Log.Logger != Serilog.Core.Logger.None)
        {
            if (fatal)
                Log.Fatal(ex, "{Caption}", caption);
            else
                Log.Error(ex, "{Caption}", caption);
        }
        else
        {
            System.Diagnostics.Trace.TraceError("{0}: {1}", caption, ex);
        }
    }
}
