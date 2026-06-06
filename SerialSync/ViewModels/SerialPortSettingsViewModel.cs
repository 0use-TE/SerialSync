using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SerialSync.Dock;
using SerialSync.Models;
using SerialSync.Services;
using System.Collections.ObjectModel;
using System.IO.Ports;

namespace SerialSync.ViewModels;

public partial class SerialPortSettingsViewModel : DockTabViewModel
{
    public override string Id => "serial-port";
    public override string Header => "串口";
    public override DockSlot DefaultSlot => DockSlot.Left;

    private readonly ISerialPortService _serial;
    private readonly ILogger<SerialPortSettingsViewModel> _logger;
    private readonly INotificationService _notifications;
    private readonly SerialPortSettingsStore _settingsStore;
    private bool _suspendSave;
    private string? _storedPortName;

    [ObservableProperty]
    private string? _selectedPort;

    [ObservableProperty]
    private int _baudRate = 115200;

    [ObservableProperty]
    private Parity _parity = Parity.None;

    [ObservableProperty]
    private int _dataBits = 8;

    [ObservableProperty]
    private StopBits _stopBits = StopBits.One;

    [ObservableProperty]
    private bool _dtrEnable;

    [ObservableProperty]
    private bool _rtsEnable;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _statusText = "未连接";

    public string ConnectButtonText => IsConnected ? "断开连接" : "连接";

    public IBrush StatusIndicatorBrush =>
        IsConnected
            ? new SolidColorBrush(Color.Parse("#4EC9B0"))
            : new SolidColorBrush(Color.Parse("#F48771"));

    public ObservableCollection<string> PortNames { get; } = new();
    public int[] BaudRates { get; } =
    [
        300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 38400, 57600, 115200, 230400, 460800, 921600,
    ];

    public Array ParityValues => Enum.GetValues<Parity>();
    public int[] DataBitsOptions { get; } = [5, 6, 7, 8];
    public Array StopBitsValues => Enum.GetValues<StopBits>();

    public SerialPortSettingsViewModel(
        ISerialPortService serial,
        ILogger<SerialPortSettingsViewModel> logger,
        INotificationService notifications,
        SerialPortSettingsStore settingsStore)
    {
        _serial = serial;
        _logger = logger;
        _notifications = notifications;
        _settingsStore = settingsStore;
        _serial.ConnectionChanged += (_, open) => UpdateConnectionState(open);

        _suspendSave = true;
        LoadStoredSettings();
        RefreshPorts();
        ApplyStoredPortSelection();
        _suspendSave = false;

        UpdateConnectionState(_serial.IsOpen);
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

        ApplyToService();
        try
        {
            _serial.Connect();
            _logger.LogInformation("已连接串口 {Port}", SelectedPort);
            _notifications.ShowSuccess("串口已连接", $"{SelectedPort} @ {BaudRate}");
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

    partial void OnSelectedPortChanged(string? value) => SaveSettings();
    partial void OnBaudRateChanged(int value) => SaveSettings();
    partial void OnParityChanged(Parity value) => SaveSettings();
    partial void OnDataBitsChanged(int value) => SaveSettings();
    partial void OnStopBitsChanged(StopBits value) => SaveSettings();
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
            BaudRate = stored.BaudRate;
        if (stored.DataBits is >= 5 and <= 8)
            DataBits = stored.DataBits;
        if (Enum.IsDefined(typeof(Parity), stored.Parity))
            Parity = (Parity)stored.Parity;
        if (Enum.IsDefined(typeof(StopBits), stored.StopBits))
            StopBits = (StopBits)stored.StopBits;
        DtrEnable = stored.DtrEnable;
        RtsEnable = stored.RtsEnable;
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

        _settingsStore.Save(new SerialPortSettingsStorage
        {
            PortName = SelectedPort,
            BaudRate = BaudRate,
            Parity = (int)Parity,
            DataBits = DataBits,
            StopBits = (int)StopBits,
            DtrEnable = DtrEnable,
            RtsEnable = RtsEnable,
        });
    }

    private void ApplyToService()
    {
        _serial.PortName = SelectedPort!;
        _serial.BaudRate = BaudRate;
        _serial.Parity = Parity;
        _serial.DataBits = DataBits;
        _serial.StopBits = StopBits;
        _serial.DtrEnable = DtrEnable;
        _serial.RtsEnable = RtsEnable;
    }

    private void UpdateConnectionState(bool open)
    {
        IsConnected = open;
        StatusText = open ? $"已连接 {_serial.PortName}" : "未连接";
        OnPropertyChanged(nameof(ConnectButtonText));
        OnPropertyChanged(nameof(StatusIndicatorBrush));
    }
}
