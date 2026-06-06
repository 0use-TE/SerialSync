using SerialSync.Models;
using System.IO.Ports;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.RegularExpressions;

namespace SerialSync.Services;

public interface ISerialPortService
{
    bool IsOpen { get; }
    string PortName { get; set; }
    int BaudRate { get; set; }
    Parity Parity { get; set; }
    int DataBits { get; set; }
    StopBits StopBits { get; set; }
    bool DtrEnable { get; set; }
    bool RtsEnable { get; set; }

    string[] GetPortNames();
    void Connect();
    void Disconnect();
    void SendText(string text, LineEnding lineEnding);
    void SendBytes(byte[] data);
    void ClearReceiveBuffer();
    IObservable<ReceiveRecord> Received { get; }
    event EventHandler<bool>? ConnectionChanged;
}

public sealed partial class SerialPortService : ISerialPortService, IDisposable
{
    private readonly SerialPort _port = new();
    private readonly Subject<ReceiveRecord> _received = new();

    public bool IsOpen => _port.IsOpen;
    public string PortName { get => _port.PortName; set => _port.PortName = value; }
    public int BaudRate { get => _port.BaudRate; set => _port.BaudRate = value; }
    public Parity Parity { get => _port.Parity; set => _port.Parity = value; }
    public int DataBits { get => _port.DataBits; set => _port.DataBits = value; }
    public StopBits StopBits { get => _port.StopBits; set => _port.StopBits = value; }
    public bool DtrEnable { get => _port.DtrEnable; set => _port.DtrEnable = value; }
    public bool RtsEnable { get => _port.RtsEnable; set => _port.RtsEnable = value; }

    public IObservable<ReceiveRecord> Received => _received.AsObservable();
    public event EventHandler<bool>? ConnectionChanged;

    public SerialPortService()
    {
        _port.BaudRate = 115200;
        _port.DataBits = 8;
        _port.Parity = Parity.None;
        _port.StopBits = StopBits.One;
        _port.ReadBufferSize = 65536;
        _port.WriteBufferSize = 65536;
        _port.DataReceived += OnDataReceived;
    }

    public string[] GetPortNames() => SerialPort.GetPortNames();

    public void Connect()
    {
        if (_port.IsOpen)
            return;

        _port.Open();
        ConnectionChanged?.Invoke(this, true);
    }

    public void Disconnect()
    {
        if (!_port.IsOpen)
            return;

        _port.Close();
        ConnectionChanged?.Invoke(this, false);
    }

    public void SendText(string text, LineEnding lineEnding)
    {
        if (string.IsNullOrEmpty(text))
            throw new InvalidOperationException("发送内容不能为空。");

        var payload = text + lineEnding switch
        {
            LineEnding.Cr => "\r",
            LineEnding.Lf => "\n",
            LineEnding.CrLf => "\r\n",
            _ => string.Empty,
        };

        SendBytes(Encoding.UTF8.GetBytes(payload));
    }

    public void SendBytes(byte[] data)
    {
        if (!_port.IsOpen)
            throw new InvalidOperationException("串口未连接。");

        // 不与 DataReceived 共用锁，避免 Write 阻塞时 UI 卡死
        _port.Write(data, 0, data.Length);
    }

    public void ClearReceiveBuffer()
    {
        if (!_port.IsOpen)
            return;

        _port.DiscardInBuffer();
    }

    public static byte[] ParseHex(string input)
    {
        var cleaned = input.Replace("0x", "", StringComparison.OrdinalIgnoreCase)
            .Replace("0X", "", StringComparison.OrdinalIgnoreCase)
            .Replace(",", " ")
            .Replace("-", " ")
            .Replace("\r", " ")
            .Replace("\n", " ");

        var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            throw new FormatException("十六进制内容为空。");

        var bytes = new byte[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!Regex.IsMatch(parts[i], "^[0-9A-Fa-f]{1,2}$"))
                throw new FormatException($"无效的十六进制字节: {parts[i]}");

            bytes[i] = Convert.ToByte(parts[i], 16);
        }

        return bytes;
    }

    public static string FormatHex(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return string.Empty;

        var sb = new StringBuilder(data.Length * 3);
        for (var i = 0; i < data.Length; i++)
        {
            if (i > 0)
                sb.Append(' ');
            sb.Append(data[i].ToString("X2"));
        }

        return sb.ToString();
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            if (!_port.IsOpen)
                return;

            var count = _port.BytesToRead;
            if (count <= 0)
                return;

            var buffer = new byte[count];
            var read = _port.Read(buffer, 0, count);
            if (read <= 0)
                return;

            if (read != buffer.Length)
                buffer = buffer.AsSpan(0, read).ToArray();

            _received.OnNext(new ReceiveRecord
            {
                Data = buffer,
                Time = DateTime.Now.TimeOfDay,
            });
        }
        catch
        {
            // 接收线程异常不应导致进程崩溃
        }
    }

    public void Dispose()
    {
        _port.DataReceived -= OnDataReceived;
        if (_port.IsOpen)
            _port.Close();
        _port.Dispose();
        _received.Dispose();
    }
}
