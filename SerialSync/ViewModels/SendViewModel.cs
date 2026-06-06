using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SerialSync.Dock;
using SerialSync.Models;
using SerialSync.Services;

namespace SerialSync.ViewModels;

public partial class SendViewModel : DockTabViewModel
{
    public override string Id => "send";
    public override string Header => "发送";
    public override DockSlot DefaultSlot => DockSlot.CenterBottom;

    private readonly ISerialPortService _serial;
    private readonly ILogger<SendViewModel> _logger;
    private readonly INotificationService _notifications;
    private readonly SendHistoryService _history;
    private readonly SerialTrafficService _traffic;
    private readonly TextEncodingService _encoding;
    private CancellationTokenSource? _periodicCts;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private SendFormat _sendFormat = SendFormat.Text;

    [ObservableProperty]
    private LineEnding _lineEnding = LineEnding.None;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _isPortOpen;

    [ObservableProperty]
    private SendRecord? _selectedHistoryItem;

    [ObservableProperty]
    private int _periodicIntervalMs = 1000;

    [ObservableProperty]
    private bool _isPeriodicSending;

    public SendHistoryService History => _history;

    public Array SendFormats => Enum.GetValues<SendFormat>();
    public Array LineEndings => Enum.GetValues<LineEnding>();
    public bool IsTextMode => SendFormat == SendFormat.Text;
    public string PeriodicSendButtonText => IsPeriodicSending ? "停止循环" : "循环发送";

    partial void OnSendFormatChanged(SendFormat value) => OnPropertyChanged(nameof(IsTextMode));

    partial void OnIsPeriodicSendingChanged(bool value) => OnPropertyChanged(nameof(PeriodicSendButtonText));

    public SendViewModel(
        ISerialPortService serial,
        ILogger<SendViewModel> logger,
        INotificationService notifications,
        SendHistoryService history,
        SerialTrafficService traffic,
        TextEncodingService encoding)
    {
        _serial = serial;
        _logger = logger;
        _notifications = notifications;
        _history = history;
        _traffic = traffic;
        _encoding = encoding;
        _serial.ConnectionChanged += OnConnectionChanged;
        UpdatePortState(_serial.IsOpen);
    }

    private void OnConnectionChanged(object? sender, bool open)
    {
        Dispatcher.UIThread.Post(() =>
        {
            UpdatePortState(open);
            if (open)
            {
                StatusText = string.Empty;
                return;
            }

            StopPeriodicSendInternal();
        });
    }

    private void UpdatePortState(bool open) => IsPortOpen = open;

    [RelayCommand]
    private async Task Send()
    {
        if (!_serial.IsOpen)
        {
            StatusText = "串口未连接";
            _notifications.ShowWarning("发送失败", "串口未连接");
            return;
        }

        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusText = "发送内容不能为空";
            _notifications.ShowWarning("发送失败", "内容不能为空");
            return;
        }

        try
        {
            await SendOnceAsync();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
            _logger.LogError(ex, "发送失败");
            _notifications.ShowError("发送失败", ex.Message);
        }
    }

    [RelayCommand]
    private async Task TogglePeriodicSend()
    {
        if (IsPeriodicSending)
        {
            StopPeriodicSendInternal();
            _notifications.ShowInfo("已停止", "循环发送");
            return;
        }

        if (!_serial.IsOpen)
        {
            _notifications.ShowWarning("无法循环发送", "串口未连接");
            return;
        }

        if (string.IsNullOrWhiteSpace(InputText))
        {
            _notifications.ShowWarning("无法循环发送", "发送内容为空");
            return;
        }

        if (PeriodicIntervalMs < 10)
        {
            _notifications.ShowWarning("无法循环发送", "间隔不能小于 10 ms");
            return;
        }

        _periodicCts?.Cancel();
        _periodicCts = new CancellationTokenSource();
        var token = _periodicCts.Token;
        IsPeriodicSending = true;
        StatusText = $"循环发送中 · 每 {PeriodicIntervalMs} ms";

        try
        {
            while (!token.IsCancellationRequested)
            {
                await SendOnceAsync(silent: true);
                await Task.Delay(PeriodicIntervalMs, token);
            }
        }
        catch (OperationCanceledException)
        {
            // expected on stop
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
            _notifications.ShowError("循环发送失败", ex.Message);
        }
        finally
        {
            IsPeriodicSending = false;
        }
    }

    private async Task SendOnceAsync(bool silent = false)
    {
        var input = InputText;
        var format = SendFormat;
        var lineEnding = LineEnding;
        var (payload, preview) = await Task.Run(() =>
            SerialSendOperations.Send(_serial, input, format, lineEnding, _encoding, _traffic));

        _history.Add(preview, input, format, lineEnding, payload.Length);
        if (!silent)
        {
            StatusText = $"已发送 {payload.Length} 字节";
            _logger.LogInformation("发送 {Bytes} 字节 ({Format})", payload.Length, format);
        }
    }

    private void StopPeriodicSendInternal()
    {
        _periodicCts?.Cancel();
        _periodicCts = null;
        if (IsPeriodicSending)
            IsPeriodicSending = false;
    }

    [RelayCommand]
    private async Task SendFile(IStorageProvider? storageProvider)
    {
        if (storageProvider is null)
        {
            StatusText = "无法打开文件选择器";
            _notifications.ShowError("发送失败", "无法打开文件选择器");
            return;
        }

        if (!_serial.IsOpen)
        {
            StatusText = "串口未连接";
            _notifications.ShowWarning("发送失败", "串口未连接");
            return;
        }

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择要发送的文件",
            AllowMultiple = false,
        });

        if (files.Count == 0)
            return;

        var filePath = files[0].TryGetLocalPath();
        await using var stream = await files[0].OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var bytes = ms.ToArray();

        try
        {
            await Task.Run(() => _serial.SendBytes(bytes));
            _traffic.AddTx(bytes.Length);
            _history.Add($"[文件] {files[0].Name}", string.Empty, SendFormat.Hex, LineEnding.None, bytes.Length, true, filePath);
            StatusText = $"已发送文件 {bytes.Length} 字节";
            _logger.LogInformation("发送文件 {File} ({Bytes} 字节)", files[0].Name, bytes.Length);
            _notifications.ShowSuccess("文件已发送", $"{files[0].Name} · {bytes.Length} 字节");
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
            _logger.LogError(ex, "发送文件失败");
            _notifications.ShowError("发送文件失败", ex.Message);
        }
    }

    [RelayCommand]
    private async Task ResendHistory(SendRecord? record)
    {
        if (record is null)
            return;

        if (!_serial.IsOpen)
        {
            _notifications.ShowWarning("重发失败", "串口未连接");
            return;
        }

        try
        {
            if (record.IsFile)
            {
                if (string.IsNullOrWhiteSpace(record.FilePath) || !File.Exists(record.FilePath))
                {
                    _notifications.ShowWarning("无法重发", "原文件路径不可用");
                    return;
                }

                var bytes = await Task.Run(() => File.ReadAllBytes(record.FilePath));
                await Task.Run(() => _serial.SendBytes(bytes));
                _traffic.AddTx(bytes.Length);
                _history.Add(record.Preview, string.Empty, record.Format, record.LineEnding, bytes.Length, true, record.FilePath);
                _notifications.ShowSuccess("已重发", record.Preview);
                return;
            }

            var (payload, preview) = await Task.Run(() =>
                SerialSendOperations.Send(_serial, record.Payload, record.Format, record.LineEnding, _encoding, _traffic));
            _history.Add(preview, record.Payload, record.Format, record.LineEnding, payload.Length);
            InputText = record.Payload;
            SendFormat = record.Format;
            LineEnding = record.LineEnding;
            StatusText = $"已重发 {payload.Length} 字节";
            _notifications.ShowSuccess("已重发", preview.Length > 32 ? preview[..32] + "…" : preview);
        }
        catch (Exception ex)
        {
            _notifications.ShowError("重发失败", ex.Message);
        }
    }

    [RelayCommand]
    private void ClearHistory() => _history.Clear();
}
