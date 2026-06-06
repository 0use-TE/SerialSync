using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SerialSync.Dock;
using SerialSync.Models;
using SerialSync.Services;

namespace SerialSync.ViewModels;

public partial class ToolsViewModel : DockTabViewModel
{
    public override string Id => "tools";
    public override string Header => "工具";
    public override DockSlot DefaultSlot => DockSlot.Right;

    private readonly SendViewModel _send;
    private readonly INotificationService _notifications;

    [ObservableProperty]
    private string _crcInputHex = "01 03 00 00 00 01";

    [ObservableProperty]
    private string _crcResult = string.Empty;

    [ObservableProperty]
    private string _modbusSlave = "01";

    [ObservableProperty]
    private string _modbusFunction = "03";

    [ObservableProperty]
    private string _modbusAddress = "0000";

    [ObservableProperty]
    private string _modbusRegisterCount = "0001";

    [ObservableProperty]
    private string _modbusFrame = string.Empty;

    public ToolsViewModel(SendViewModel send, INotificationService notifications)
    {
        _send = send;
        _notifications = notifications;
    }

    [RelayCommand]
    private void CalculateCrc()
    {
        try
        {
            var data = SerialDataFormat.ParseHex(CrcInputHex);
            var crc = ModbusCrc.Crc16(data);
            CrcResult = $"{crc:X4}  (低字节 {crc & 0xFF:X2}  高字节 {crc >> 8:X2})";
        }
        catch (Exception ex)
        {
            CrcResult = ex.Message;
        }
    }

    [RelayCommand]
    private void BuildModbusRead()
    {
        try
        {
            var slave = ParseByteHex(ModbusSlave, "从站地址");
            var function = ParseByteHex(ModbusFunction, "功能码");
            var addr = ParseUShortHex(ModbusAddress, "寄存器地址");
            var count = ParseUShortHex(ModbusRegisterCount, "寄存器数量");

            Span<byte> frame = stackalloc byte[6];
            frame[0] = slave;
            frame[1] = function;
            frame[2] = (byte)(addr >> 8);
            frame[3] = (byte)(addr & 0xFF);
            frame[4] = (byte)(count >> 8);
            frame[5] = (byte)(count & 0xFF);

            var withCrc = ModbusCrc.AppendCrc(frame);
            ModbusFrame = ModbusCrc.FormatHex(withCrc);
            CrcInputHex = ModbusCrc.FormatHex(frame);
            CalculateCrc();
        }
        catch (Exception ex)
        {
            ModbusFrame = ex.Message;
        }
    }

    [RelayCommand]
    private void FillSendFromModbus()
    {
        if (string.IsNullOrWhiteSpace(ModbusFrame))
        {
            _notifications.ShowWarning("无法填入", "请先生成 Modbus 帧");
            return;
        }

        _send.InputText = ModbusFrame.Replace(" ", string.Empty);
        _send.SendFormat = SendFormat.Hex;
        _notifications.ShowSuccess("已填入", "发送面板");
    }

    [RelayCommand]
    private void FillSendFromCrcInput()
    {
        try
        {
            var data = SerialDataFormat.ParseHex(CrcInputHex);
            var withCrc = ModbusCrc.AppendCrc(data);
            _send.InputText = ModbusCrc.FormatHex(withCrc).Replace(" ", string.Empty);
            _send.SendFormat = SendFormat.Hex;
            _notifications.ShowSuccess("已填入", "发送面板（含 CRC）");
        }
        catch (Exception ex)
        {
            _notifications.ShowError("填入失败", ex.Message);
        }
    }

    private static byte ParseByteHex(string text, string field)
    {
        var cleaned = text.Trim();
        if (!byte.TryParse(cleaned, System.Globalization.NumberStyles.HexNumber, null, out var value))
            throw new FormatException($"{field} 无效");
        return value;
    }

    private static ushort ParseUShortHex(string text, string field)
    {
        var cleaned = text.Trim();
        if (!ushort.TryParse(cleaned, System.Globalization.NumberStyles.HexNumber, null, out var value))
            throw new FormatException($"{field} 无效");
        return value;
    }
}
