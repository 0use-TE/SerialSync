using SerialSync.Models;
using System.IO.Ports;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SerialSync.Services;

public sealed partial class SerialPortService : ISerialPortService, IDisposable
{
    private readonly SerialPort _port = new();
    private readonly Subject<ReceiveRecord> _received = new();
    private readonly TextEncodingService _encoding;

    public bool IsOpen => _port.IsOpen;
    public string PortName { get => _port.PortName; set => _port.PortName = value; }
    public int BaudRate { get => _port.BaudRate; set => _port.BaudRate = value; }
    public Parity Parity { get => _port.Parity; set => _port.Parity = value; }
    public int DataBits { get => _port.DataBits; set => _port.DataBits = value; }
    public StopBits StopBits { get => _port.StopBits; set => _port.StopBits = value; }
    public Handshake Handshake { get => _port.Handshake; set => _port.Handshake = value; }
    public int ReadTimeout { get => _port.ReadTimeout; set => _port.ReadTimeout = value; }
    public int WriteTimeout { get => _port.WriteTimeout; set => _port.WriteTimeout = value; }
    public bool DtrEnable { get => _port.DtrEnable; set => _port.DtrEnable = value; }
    public bool RtsEnable { get => _port.RtsEnable; set => _port.RtsEnable = value; }
    public bool CtsHolding => _port.IsOpen && _port.CtsHolding;
    public bool DsrHolding => _port.IsOpen && _port.DsrHolding;
    public bool CdHolding => _port.IsOpen && _port.CDHolding;

    public IObservable<ReceiveRecord> Received => _received.AsObservable();
    public event EventHandler<bool>? ConnectionChanged;

    public SerialPortService(TextEncodingService encoding)
    {
        _encoding = encoding;
        _port.BaudRate = 115200;
        _port.DataBits = 8;
        _port.Parity = Parity.None;
        _port.StopBits = StopBits.One;
        _port.ReadBufferSize = 65536;
        _port.WriteBufferSize = 65536;
        _port.ReadTimeout = -1;
        _port.WriteTimeout = -1;
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

        SendBytes(_encoding.Encode(payload));
    }

    public void SendBytes(byte[] data)
    {
        if (!_port.IsOpen)
            throw new InvalidOperationException("串口未连接。");

        _port.Write(data, 0, data.Length);
    }

    public void SendBreak()
    {
        if (!_port.IsOpen)
            throw new InvalidOperationException("串口未连接。");

        _port.BreakState = true;
        Thread.Sleep(250);
        _port.BreakState = false;
    }

    public void ClearReceiveBuffer()
    {
        if (!_port.IsOpen)
            return;

        _port.DiscardInBuffer();
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
