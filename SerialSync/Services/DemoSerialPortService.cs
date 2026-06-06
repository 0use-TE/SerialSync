#if BROWSER
using SerialSync.Models;
using System.IO.Ports;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace SerialSync.Services;

public sealed class DemoSerialPortService : ISerialPortService, IDisposable
{
    private static readonly string[] DemoPorts =
    [
        "COM3 (演示)",
        "COM4 (演示)",
        "USB 虚拟串口 (演示)",
    ];

    private readonly Subject<ReceiveRecord> _received = new();
    private readonly TextEncodingService _encoding;
    private CancellationTokenSource? _demoCts;
    private string _portName = DemoPorts[0];
    private int _baudRate = 115200;
    private Parity _parity = Parity.None;
    private int _dataBits = 8;
    private StopBits _stopBits = StopBits.One;
    private Handshake _handshake = Handshake.None;
    private int _readTimeout = -1;
    private int _writeTimeout = -1;
    private bool _dtrEnable;
    private bool _rtsEnable;

    public DemoSerialPortService(TextEncodingService encoding) => _encoding = encoding;

    public bool IsOpen { get; private set; }
    public string PortName { get => _portName; set => _portName = value; }
    public int BaudRate { get => _baudRate; set => _baudRate = value; }
    public Parity Parity { get => _parity; set => _parity = value; }
    public int DataBits { get => _dataBits; set => _dataBits = value; }
    public StopBits StopBits { get => _stopBits; set => _stopBits = value; }
    public Handshake Handshake { get => _handshake; set => _handshake = value; }
    public int ReadTimeout { get => _readTimeout; set => _readTimeout = value; }
    public int WriteTimeout { get => _writeTimeout; set => _writeTimeout = value; }
    public bool DtrEnable { get => _dtrEnable; set => _dtrEnable = value; }
    public bool RtsEnable { get => _rtsEnable; set => _rtsEnable = value; }
    public bool CtsHolding => IsOpen;
    public bool DsrHolding => IsOpen;
    public bool CdHolding => IsOpen;

    public IObservable<ReceiveRecord> Received => _received.AsObservable();
    public event EventHandler<bool>? ConnectionChanged;

    public string[] GetPortNames() => DemoPorts;

    public void Connect()
    {
        if (IsOpen)
            return;

        IsOpen = true;
        ConnectionChanged?.Invoke(this, true);
        StartDemoTraffic();
    }

    public void Disconnect()
    {
        if (!IsOpen)
            return;

        StopDemoTraffic();
        IsOpen = false;
        ConnectionChanged?.Invoke(this, false);
    }

    public void SendText(string text, LineEnding lineEnding)
    {
        if (!IsOpen)
            throw new InvalidOperationException("串口未连接");

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
        if (!IsOpen)
            throw new InvalidOperationException("串口未连接");

        _received.OnNext(new ReceiveRecord { Data = data, Time = DateTime.Now.TimeOfDay });
    }

    public void SendBreak() { }

    public void ClearReceiveBuffer() { }

    private void StartDemoTraffic()
    {
        _demoCts?.Cancel();
        _demoCts = new CancellationTokenSource();
        var token = _demoCts.Token;

        _ = Task.Run(async () =>
        {
            await Task.Delay(800, token);
            PushDemoLine("SerialSync Web 预览 · 模拟设备已连接", token);
            await Task.Delay(2200, token);

            while (!token.IsCancellationRequested)
            {
                PushDemoLine($"[{DateTime.Now:HH:mm:ss}] demo rx line", token);
                await Task.Delay(4000, token);
            }
        }, token);
    }

    private void PushDemoLine(string text, CancellationToken token)
    {
        if (token.IsCancellationRequested || !IsOpen)
            return;

        _received.OnNext(new ReceiveRecord
        {
            Data = _encoding.Encode(text + "\r\n"),
            Time = DateTime.Now.TimeOfDay,
        });
    }

    private void StopDemoTraffic()
    {
        _demoCts?.Cancel();
        _demoCts = null;
    }

    public void Dispose()
    {
        StopDemoTraffic();
        _received.Dispose();
    }
}
#endif
