using SerialSync.Models;
using System.IO.Ports;

namespace SerialSync.Services;

public interface ISerialPortService
{
    bool IsOpen { get; }
    string PortName { get; set; }
    int BaudRate { get; set; }
    Parity Parity { get; set; }
    int DataBits { get; set; }
    StopBits StopBits { get; set; }
    Handshake Handshake { get; set; }
    int ReadTimeout { get; set; }
    int WriteTimeout { get; set; }
    bool DtrEnable { get; set; }
    bool RtsEnable { get; set; }
    bool CtsHolding { get; }
    bool DsrHolding { get; }
    bool CdHolding { get; }

    string[] GetPortNames();
    void Connect();
    void Disconnect();
    void SendText(string text, LineEnding lineEnding);
    void SendBytes(byte[] data);
    void SendBreak();
    void ClearReceiveBuffer();
    IObservable<ReceiveRecord> Received { get; }
    event EventHandler<bool>? ConnectionChanged;
}
