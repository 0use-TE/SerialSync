using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog.Events;
using SerialSync.Dock;
using SerialSync.Models;
using SerialSync.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Linq;

namespace SerialSync.ViewModels;

public partial class LogViewModel : DockTabViewModel, IDisposable
{
    public override string Id => "log";
    public override string Header => "日志";
    public override DockSlot DefaultSlot => DockSlot.Right;
    public sealed class LogLine
    {
        public required string Time { get; init; }
        public required string Level { get; init; }
        public required string Message { get; init; }
        public required IBrush LevelBrush { get; init; }
    }

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<LogLine> Lines { get; } = new();

    private readonly List<LogLine> _all = new();
    private readonly IDisposable _subscription;
    private readonly LayoutStore _layoutStore;

    public LogViewModel(InMemoryLogSink sink, LayoutStore layoutStore)
    {
        _layoutStore = layoutStore;
        _subscription = sink.Stream
            .Select(e => new LogLine
            {
                Time = e.Timestamp.ToString("HH:mm:ss"),
                Level = e.Level.ToString().ToUpperInvariant(),
                Message = e.RenderMessage(),
                LevelBrush = new SolidColorBrush(Color.Parse(GetColor(e.Level))),
            })
            .Subscribe(line =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _all.Add(line);
                    if (PassesFilter(line))
                        Lines.Add(line);
                });
            });
    }

    [RelayCommand]
    private void Clear()
    {
        _all.Clear();
        Lines.Clear();
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        Directory.CreateDirectory(_layoutStore.LogFolder);
        Process.Start(new ProcessStartInfo
        {
            FileName = _layoutStore.LogFolder,
            UseShellExecute = true,
        });
    }

    partial void OnSearchTextChanged(string value)
    {
        Lines.Clear();
        foreach (var line in _all.Where(PassesFilter))
            Lines.Add(line);
    }

    private bool PassesFilter(LogLine line) =>
        string.IsNullOrWhiteSpace(SearchText) ||
        line.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

    private static string GetColor(LogEventLevel level) => level switch
    {
        LogEventLevel.Debug => "#4F46E5",
        LogEventLevel.Information => "#0EA5E9",
        LogEventLevel.Warning => "#D97706",
        LogEventLevel.Error => "#DC2626",
        LogEventLevel.Fatal => "#B91C1C",
        _ => "#667085",
    };

    public void Dispose() => _subscription.Dispose();
}
