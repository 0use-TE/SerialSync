using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SerialSync.Dock;
using SerialSync.Models;
using SerialSync.Services;
using System.Collections.ObjectModel;
using System.IO.Ports;

namespace SerialSync.ViewModels;

public partial class SerialPortSettingsViewModel : DockTabViewModel, IDisposable
{
    public override string Id => "serial-port";
    public override string Header => "串口";
    public override DockSlot DefaultSlot => DockSlot.Left;

    private readonly ISerialPortService _serial;
    private readonly ILogger<SerialPortSettingsViewModel> _logger;
    private readonly INotificationService _notifications;
    private readonly SerialPortSettingsStore _settingsStore;
    private readonly TextEncodingService _encoding;
    private readonly IAppHostEnvironment _host;
    private readonly DispatcherTimer _lineStatusTimer;
    private bool _suspendSave;
    private string? _storedPortName;

    [ObservableProperty]
    private string? _selectedPort;

    [ObservableProperty]
    private string _baudRateText = "115200";

    [ObservableProperty]
    private Parity _parity = Parity.None;

    [ObservableProperty]
    private int _dataBits = 8;

    [ObservableProperty]
    private StopBits _stopBits = StopBits.One;

    [ObservableProperty]
    private Handshake _handshake = Handshake.None;

    [ObservableProperty]
    private int _readTimeout = -1;

    [ObservableProperty]
    private int _writeTimeout = -1;

    [ObservableProperty]
    private bool _dtrEnable;

    [ObservableProperty]
    private bool _rtsEnable;

    [ObservableProperty]
    private bool _autoConnectOnStartup;

    [ObservableProperty]
    private TextEncodingKind _textEncoding = TextEncodingKind.Utf8;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _statusText = "未连接";

    [ObservableProperty]
    private string _lineStatusText = string.Empty;

    public string ConnectButtonText => IsConnected ? "断开连接" : "连接";

    public IBrush StatusIndicatorBrush =>
        IsConnected
            ? new SolidColorBrush(Color.Parse("#4EC9B0"))
            : new SolidColorBrush(Color.Parse("#F48771"));

    public ObservableCollection<string> PortNames { get; } = new();
    public int[] BaudRates { get; } =
    [
        300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 38400, 57600, 115200, 230400, 460800, 921600, 1500000,
    ];

    public Array ParityValues => Enum.GetValues<Parity>();
    public int[] DataBitsOptions { get; } = [5, 6, 7, 8];
    public Array StopBitsValues => Enum.GetValues<StopBits>();
    public Array HandshakeValues => Enum.GetValues<Handshake>();
    public Array TextEncodings => Enum.GetValues<TextEncodingKind>();

    public SerialPortSettingsViewModel(
        ISerialPortService serial,
        ILogger<SerialPortSettingsViewModel> logger,
        INotificationService notifications,
        SerialPortSettingsStore settingsStore,
        TextEncodingService encoding,
        IAppHostEnvironment host)
    {
        _serial = serial;
        _logger = logger;
        _notifications = notifications;
        _settingsStore = settingsStore;
        _encoding = encoding;
        _host = host;
        _serial.ConnectionChanged += (_, open) => UpdateConnectionState(open);

        _lineStatusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _lineStatusTimer.Tick += (_, _) => RefreshLineStatus();

        _suspendSave = true;
        LoadStoredSettings();
        RefreshPorts();
        ApplyStoredPortSelection();
        _suspendSave = false;

        UpdateConnectionState(_serial.IsOpen);
    }

    public Task TryAutoConnectOnStartupAsync()
    {
        if (_host.IsBrowserPreview || !AutoConnectOnStartup || _serial.IsOpen)
            return Task.CompletedTask;

        if (SelectedPort is null || SelectedPort == "未检测到串口")
            return Task.CompletedTask;

        try
        {
            ApplyToService();
            _serial.Connect();
            _notifications.ShowSuccess("自动连接", $"{SelectedPort} @ {BaudRateText}");
            SaveSettings();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "自动连接失败");
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void RefreshPorts()
    {
        PortNames.Clear();
        foreach (var name in _serial.GetPortNames())
            PortNames.Add(name);

        if (PortNames.Count == 0)
        {
            PortNames.Add("未检测到串口");
            SelectedPort = PortNames[0];
            return;
        }

        if (SelectedPort is null || SelectedPort == "未检测到串口" || !PortNames.Contains(SelectedPort))
            SelectedPort = PortNames[0];
    }

    [RelayCommand]
    private void ToggleConnection()
    {
        if (_serial.IsOpen)
        {
            var port = _serial.PortName;
            _serial.Disconnect();
            _logger.LogInformation("串口已断开");
            _notifications.ShowInfo("已断开", port);
            SaveSettings();
            return;
        }

        if (SelectedPort is null || SelectedPort == "未检测到串口")
        {
            StatusText = "请先选择有效串口";
            _notifications.ShowWarning("无法连接", "请先选择有效串口");
            return;
        }

        if (!TryParseBaudRate(out var baud))
        {
            _notifications.ShowWarning("无法连接", "波特率无效");
            return;
        }

        ApplyToService(baud);
        try
        {
            _serial.Connect();
            _logger.LogInformation("已连接串口 {Port}", SelectedPort);
            _notifications.ShowSuccess("串口已连接", $"{SelectedPort} @ {baud}");
            SaveSettings();
        }
        catch (Exception ex)
        {
            StatusText = $"连接失败: {ex.Message}";
            _logger.LogError(ex, "连接串口失败");
            _notifications.ShowError("连接失败", ex.Message);
            RefreshPorts();
        }
    }

    [RelayCommand]
    private void SendBreak()
    {
        if (!_serial.IsOpen)
        {
            _notifications.ShowWarning("无法发送 Break", "串口未连接");
            return;
        }

        try
        {
            _serial.SendBreak();
            _notifications.ShowInfo("已发送", "Break 信号");
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Break 失败", ex.Message);
        }
    }

    [RelayCommand]
    private void SelectBaudRate(int rate) => BaudRateText = rate.ToString();

    partial void OnSelectedPortChanged(string? value) => SaveSettings();
    partial void OnBaudRateTextChanged(string value) => SaveSettings();
    partial void OnParityChanged(Parity value) => SaveSettings();
    partial void OnDataBitsChanged(int value) => SaveSettings();
    partial void OnStopBitsChanged(StopBits value) => SaveSettings();
    partial void OnHandshakeChanged(Handshake value) => SaveSettings();
    partial void OnReadTimeoutChanged(int value) => SaveSettings();
    partial void OnWriteTimeoutChanged(int value) => SaveSettings();
    partial void OnAutoConnectOnStartupChanged(bool value) => SaveSettings();

    partial void OnTextEncodingChanged(TextEncodingKind value)
    {
        _encoding.Kind = value;
        SaveSettings();
    }

    partial void OnDtrEnableChanged(bool value)
    {
        if (_serial.IsOpen)
            _serial.DtrEnable = value;
        SaveSettings();
    }

    partial void OnRtsEnableChanged(bool value)
    {
        if (_serial.IsOpen)
            _serial.RtsEnable = value;
        SaveSettings();
    }

    private void LoadStoredSettings()
    {
        var stored = _settingsStore.Load();
        if (stored.BaudRate > 0)
            BaudRateText = stored.BaudRate.ToString();
        if (stored.DataBits is >= 5 and <= 8)
            DataBits = stored.DataBits;
        if (Enum.IsDefined(typeof(Parity), stored.Parity))
            Parity = (Parity)stored.Parity;
        if (Enum.IsDefined(typeof(StopBits), stored.StopBits))
            StopBits = (StopBits)stored.StopBits;
        if (Enum.IsDefined(typeof(Handshake), stored.Handshake))
            Handshake = (Handshake)stored.Handshake;
        ReadTimeout = stored.ReadTimeout;
        WriteTimeout = stored.WriteTimeout;
        DtrEnable = stored.DtrEnable;
        RtsEnable = stored.RtsEnable;
        AutoConnectOnStartup = stored.AutoConnectOnStartup;
        if (Enum.IsDefined(typeof(TextEncodingKind), stored.TextEncoding))
            TextEncoding = (TextEncodingKind)stored.TextEncoding;
        _encoding.Kind = TextEncoding;
        _storedPortName = stored.PortName;
    }

    private void ApplyStoredPortSelection()
    {
        if (string.IsNullOrWhiteSpace(_storedPortName))
            return;

        if (PortNames.Contains(_storedPortName))
            SelectedPort = _storedPortName;
    }

    private void SaveSettings()
    {
        if (_suspendSave || SelectedPort == "未检测到串口")
            return;

        _ = int.TryParse(BaudRateText, out var baud);

        _settingsStore.Save(new SerialPortSettingsStorage
        {
            PortName = SelectedPort,
            BaudRate = baud > 0 ? baud : 115200,
            Parity = (int)Parity,
            DataBits = DataBits,
            StopBits = (int)StopBits,
            Handshake = (int)Handshake,
            ReadTimeout = ReadTimeout,
            WriteTimeout = WriteTimeout,
            DtrEnable = DtrEnable,
            RtsEnable = RtsEnable,
            AutoConnectOnStartup = AutoConnectOnStartup,
            TextEncoding = (int)TextEncoding,
        });
    }

    private bool TryParseBaudRate(out int baud) =>
        int.TryParse(BaudRateText, out baud) && baud > 0;

    private void ApplyToService(int? baudOverride = null)
    {
        _serial.PortName = SelectedPort!;
        _serial.BaudRate = baudOverride ?? (TryParseBaudRate(out var b) ? b : 115200);
        _serial.Parity = Parity;
        _serial.DataBits = DataBits;
        _serial.StopBits = StopBits;
        _serial.Handshake = Handshake;
        _serial.ReadTimeout = ReadTimeout;
        _serial.WriteTimeout = WriteTimeout;
        _serial.DtrEnable = DtrEnable;
        _serial.RtsEnable = RtsEnable;
    }

    private void UpdateConnectionState(bool open)
    {
        IsConnected = open;
        StatusText = open ? $"已连接 {_serial.PortName}" : "未连接";
        OnPropertyChanged(nameof(ConnectButtonText));
        OnPropertyChanged(nameof(StatusIndicatorBrush));

        if (open)
        {
            _lineStatusTimer.Start();
            RefreshLineStatus();
        }
        else
        {
            _lineStatusTimer.Stop();
            LineStatusText = string.Empty;
        }
    }

    private void RefreshLineStatus()
    {
        if (!_serial.IsOpen)
            return;

        LineStatusText = $"CTS:{(_serial.CtsHolding ? "1" : "0")}  DSR:{(_serial.DsrHolding ? "1" : "0")}  CD:{(_serial.CdHolding ? "1" : "0")}";
    }

    public void Dispose() => _lineStatusTimer.Stop();
}
