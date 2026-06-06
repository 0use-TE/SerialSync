using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SerialSync.Dock;
using SerialSync.Models;
using SerialSync.Services;
using System.Text;

namespace SerialSync.ViewModels;

public partial class ReceiveViewModel : DockTabViewModel, IDisposable
{
    public override string Id => "receive";
    public override string Header => "接收";
    public override DockSlot DefaultSlot => DockSlot.CenterTop;

    private readonly ISerialPortService _serial;
    private readonly ILogger<ReceiveViewModel> _logger;
    private readonly INotificationService _notifications;
    private readonly SerialTrafficService _traffic;
    private readonly TextEncodingService _encoding;
    private readonly LayoutStore _layoutStore;
    private readonly StringBuilder _textBuffer = new();
    private readonly List<byte> _rawBuffer = new();
    private readonly IDisposable _subscription;
    private bool _suspendSave;

    [ObservableProperty]
    private ReceiveFormat _displayFormat = ReceiveFormat.Text;

    [ObservableProperty]
    private string _displayText = string.Empty;

    [ObservableProperty]
    private bool _autoScroll = true;

    [ObservableProperty]
    private bool _showTimestamp;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public long TotalBytes => _traffic.TotalRxBytes;
    public string DisplayFormatLabel => DisplayFormat == ReceiveFormat.Hex ? "切换文本" : "切换 HEX";
    public string PauseButtonText => IsPaused ? "继续" : "暂停";

    public ReceiveViewModel(
        ISerialPortService serial,
        ILogger<ReceiveViewModel> logger,
        INotificationService notifications,
        SerialTrafficService traffic,
        TextEncodingService encoding,
        LayoutStore layoutStore)
    {
        _serial = serial;
        _logger = logger;
        _notifications = notifications;
        _traffic = traffic;
        _encoding = encoding;
        _layoutStore = layoutStore;

        _suspendSave = true;
        var layout = _layoutStore.Load();
        ShowTimestamp = layout.ReceiveShowTimestamp;
        AutoScroll = layout.ReceiveAutoScroll;
        _suspendSave = false;

        _traffic.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SerialTrafficService.TotalRxBytes))
                OnPropertyChanged(nameof(TotalBytes));
        };
        _subscription = _serial.Received.Subscribe(OnReceived);
    }

    [RelayCommand]
    private void Clear()
    {
        _textBuffer.Clear();
        _rawBuffer.Clear();
        _traffic.ResetRx();
        OnPropertyChanged(nameof(TotalBytes));
        DisplayText = string.Empty;
        _serial.ClearReceiveBuffer();
        _logger.LogInformation("接收区已清空");
    }

    [RelayCommand]
    private void ToggleFormat()
    {
        DisplayFormat = DisplayFormat == ReceiveFormat.Text ? ReceiveFormat.Hex : ReceiveFormat.Text;
        RefreshDisplay();
    }

    [RelayCommand]
    private void TogglePause()
    {
        IsPaused = !IsPaused;
        OnPropertyChanged(nameof(PauseButtonText));
        if (!IsPaused)
            RefreshDisplay();
    }

    [RelayCommand]
    private async Task SaveToFile(IStorageProvider? storageProvider)
    {
        if (storageProvider is null)
        {
            _notifications.ShowError("无法保存", "无法打开文件选择器");
            return;
        }

        if (_rawBuffer.Count == 0)
        {
            _notifications.ShowWarning("无法保存", "接收区为空");
            return;
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var isText = DisplayFormat == ReceiveFormat.Text;
        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存接收数据",
            SuggestedFileName = isText ? $"receive_{timestamp}.txt" : $"receive_{timestamp}.bin",
            FileTypeChoices =
            [
                new FilePickerFileType("二进制") { Patterns = ["*.bin"] },
                new FilePickerFileType("文本") { Patterns = ["*.txt"] },
            ],
        });

        if (file is null)
            return;

        try
        {
            await using var stream = await file.OpenWriteAsync();
            if (isText)
            {
                var text = _textBuffer.ToString();
                var bytes = _encoding.GetEncoding().GetBytes(text);
                await stream.WriteAsync(bytes);
            }
            else
            {
                await stream.WriteAsync(_rawBuffer.ToArray());
            }

            await stream.FlushAsync();
            _notifications.ShowSuccess("已保存", file.Name);
        }
        catch (Exception ex)
        {
            _notifications.ShowError("保存失败", ex.Message);
        }
    }

    partial void OnDisplayFormatChanged(ReceiveFormat value)
    {
        OnPropertyChanged(nameof(DisplayFormatLabel));
        RefreshDisplay();
    }

    partial void OnShowTimestampChanged(bool value) => SaveReceivePrefs();
    partial void OnAutoScrollChanged(bool value) => SaveReceivePrefs();
    partial void OnSearchTextChanged(string value) => RefreshDisplay();

    partial void OnIsPausedChanged(bool value) => OnPropertyChanged(nameof(PauseButtonText));

    private void OnReceived(ReceiveRecord record)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _rawBuffer.AddRange(record.Data);
            _traffic.AddRx(record.Data.Length);

            var chunk = DisplayFormat == ReceiveFormat.Hex
                ? SerialDataFormat.FormatHex(record.Data)
                : _encoding.Decode(record.Data);

            if (ShowTimestamp)
            {
                var prefix = $"[{DateTime.Now:HH:mm:ss.fff}] ";
                _textBuffer.Append(prefix).Append(chunk);
            }
            else
            {
                _textBuffer.Append(chunk);
            }

            if (!IsPaused)
                RefreshDisplay();
        });
    }

    private void RefreshDisplay()
    {
        var text = DisplayFormat switch
        {
            ReceiveFormat.Hex => SerialDataFormat.FormatHex(_rawBuffer.ToArray()),
            _ => _textBuffer.ToString(),
        };

        if (string.IsNullOrWhiteSpace(SearchText))
            DisplayText = text;
        else
            DisplayText = FilterText(text, SearchText);
    }

    private static string FilterText(string text, string query)
    {
        var lines = text.Split('\n');
        return string.Join('\n', lines.Where(l => l.Contains(query, StringComparison.OrdinalIgnoreCase)));
    }

    private void SaveReceivePrefs()
    {
        if (_suspendSave)
            return;

        var layout = _layoutStore.Load();
        layout.ReceiveShowTimestamp = ShowTimestamp;
        layout.ReceiveAutoScroll = AutoScroll;
        _layoutStore.Save(layout);
    }

    public void Dispose() => _subscription.Dispose();
}
