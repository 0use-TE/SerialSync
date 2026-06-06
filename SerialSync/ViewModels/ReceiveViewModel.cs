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
    private readonly StringBuilder _textBuffer = new();
    private readonly List<byte> _rawBuffer = new();
    private readonly IDisposable _subscription;

    [ObservableProperty]
    private ReceiveFormat _displayFormat = ReceiveFormat.Text;

    [ObservableProperty]
    private string _displayText = string.Empty;

    [ObservableProperty]
    private bool _autoScroll = true;

    public long TotalBytes => _traffic.TotalRxBytes;
    public string DisplayFormatLabel => DisplayFormat == ReceiveFormat.Hex ? "切换文本" : "切换 HEX";

    public ReceiveViewModel(
        ISerialPortService serial,
        ILogger<ReceiveViewModel> logger,
        INotificationService notifications,
        SerialTrafficService traffic)
    {
        _serial = serial;
        _logger = logger;
        _notifications = notifications;
        _traffic = traffic;
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
                var bytes = Encoding.UTF8.GetBytes(text);
                await stream.WriteAsync(bytes);
            }
            else
            {
                await stream.WriteAsync(_rawBuffer.ToArray());
            }

            await stream.FlushAsync();
            _notifications.ShowSuccess("已保存", file.Name);
            _logger.LogInformation("接收数据已保存 {File}", file.Name);
        }
        catch (Exception ex)
        {
            _notifications.ShowError("保存失败", ex.Message);
            _logger.LogError(ex, "保存接收数据失败");
        }
    }

    partial void OnDisplayFormatChanged(ReceiveFormat value)
    {
        OnPropertyChanged(nameof(DisplayFormatLabel));
        RefreshDisplay();
    }

    private void OnReceived(ReceiveRecord record)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _rawBuffer.AddRange(record.Data);
            _traffic.AddRx(record.Data.Length);
            _textBuffer.Append(Encoding.UTF8.GetString(record.Data));
            RefreshDisplay();
        });
    }

    private void RefreshDisplay()
    {
        DisplayText = DisplayFormat switch
        {
            ReceiveFormat.Hex => SerialPortService.FormatHex(_rawBuffer.ToArray()),
            _ => _textBuffer.ToString(),
        };
    }

    public void Dispose() => _subscription.Dispose();
}
