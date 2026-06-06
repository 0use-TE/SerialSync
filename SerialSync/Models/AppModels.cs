using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SerialSync.Models;

public enum LineEnding
{
    None,
    Cr,
    Lf,
    CrLf,
}

public enum SendFormat
{
    Text,
    Hex,
}

public enum ReceiveFormat
{
    Text,
    Hex,
}

public sealed class SendRecord
{
    public required string Preview { get; init; }
    public required string Payload { get; init; }
    public required SendFormat Format { get; init; }
    public LineEnding LineEnding { get; init; }
    public required int ByteCount { get; init; }
    public required TimeSpan Time { get; init; }
    public bool IsFile { get; init; }
    public string? FilePath { get; init; }
}

public sealed class QuickCommand
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public SendFormat Format { get; set; } = SendFormat.Text;
    public LineEnding LineEnding { get; set; } = LineEnding.None;
}

public sealed class SendSequenceStep
{
    public string Payload { get; set; } = string.Empty;
    public SendFormat Format { get; set; } = SendFormat.Text;
    public LineEnding LineEnding { get; set; } = LineEnding.None;
    public int DelayAfterMs { get; set; }
}

public partial class SendSequence : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;

    private ObservableCollection<SendSequenceStep> _steps = new();

    public SendSequence() => HookSteps(_steps);

    public ObservableCollection<SendSequenceStep> Steps
    {
        get => _steps;
        set
        {
            UnhookSteps(_steps);
            if (SetProperty(ref _steps, value))
            {
                HookSteps(_steps);
                OnPropertyChanged(nameof(StepCount));
            }
        }
    }

    public int StepCount => Steps.Count;

    private void HookSteps(ObservableCollection<SendSequenceStep> steps) =>
        steps.CollectionChanged += OnStepsChanged;

    private void UnhookSteps(ObservableCollection<SendSequenceStep> steps) =>
        steps.CollectionChanged -= OnStepsChanged;

    private void OnStepsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        OnPropertyChanged(nameof(StepCount));
}

public sealed class SendPresetStorage
{
    public List<QuickCommand> QuickCommands { get; set; } = [];
    public List<SendSequence> Sequences { get; set; } = [];
}

public sealed class ReceiveRecord
{
    public required byte[] Data { get; init; }
    public required TimeSpan Time { get; init; }
}

public enum DockSlot
{
    Left,
    CenterTop,
    CenterBottom,
    Right,
}

public sealed class LayoutStorage
{
    public double LeftWidth { get; set; } = 220;
    public double RightWidth { get; set; } = 280;
    public double CenterTopBottomProportion { get; set; } = 1.375;
    public bool IsDarkMode { get; set; } = true;
    public bool IsTutorialOpen { get; set; } = true;
    public List<string> LeftItems { get; set; } = ["serial-port"];
    public List<string> RightItems { get; set; } = ["log", "tools"];
    public List<string> CenterTopItems { get; set; } = ["receive"];
    public List<string> CenterBottomItems { get; set; } = ["send", "quick-send", "sequence"];
    public bool ReceiveShowTimestamp { get; set; }
    public bool ReceiveAutoScroll { get; set; } = true;
    public string? LeftSelectedId { get; set; }
    public string? RightSelectedId { get; set; }
    public string? CenterTopSelectedId { get; set; }
    public string? CenterBottomSelectedId { get; set; }
}

public sealed class SerialPortSettingsStorage
{
    public string? PortName { get; set; }
    public int BaudRate { get; set; } = 115200;
    public int Parity { get; set; }
    public int DataBits { get; set; } = 8;
    public int StopBits { get; set; } = 1;
    public int Handshake { get; set; }
    public int ReadTimeout { get; set; } = -1;
    public int WriteTimeout { get; set; } = -1;
    public bool DtrEnable { get; set; }
    public bool RtsEnable { get; set; }
    public bool AutoConnectOnStartup { get; set; }
    public int TextEncoding { get; set; }
}
