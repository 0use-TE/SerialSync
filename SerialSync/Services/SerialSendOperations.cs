using SerialSync.Models;
using System.Text;

namespace SerialSync.Services;

public static class SerialSendOperations
{
    public static (byte[] Payload, string Preview) Send(
        ISerialPortService serial,
        string payload,
        SendFormat format,
        LineEnding lineEnding,
        SerialTrafficService? traffic = null)
    {
        if (!serial.IsOpen)
            throw new InvalidOperationException("串口未连接");

        byte[] data;
        string preview;

        if (format == SendFormat.Hex)
        {
            data = SerialPortService.ParseHex(payload);
            preview = SerialPortService.FormatHex(data);
            serial.SendBytes(data);
        }
        else
        {
            serial.SendText(payload, lineEnding);
            data = Encoding.UTF8.GetBytes(payload + lineEnding switch
            {
                LineEnding.Cr => "\r",
                LineEnding.Lf => "\n",
                LineEnding.CrLf => "\r\n",
                _ => string.Empty,
            });
            preview = payload;
        }

        traffic?.AddTx(data.Length);
        return (data, preview);
    }
}
