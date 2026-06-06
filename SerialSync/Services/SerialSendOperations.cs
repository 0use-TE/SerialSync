using SerialSync.Models;

namespace SerialSync.Services;

public static class SerialSendOperations
{
    public static (byte[] Payload, string Preview) Send(
        ISerialPortService serial,
        string payload,
        SendFormat format,
        LineEnding lineEnding,
        TextEncodingService encoding,
        SerialTrafficService? traffic = null)
    {
        if (!serial.IsOpen)
            throw new InvalidOperationException("串口未连接");

        byte[] data;
        string preview;

        if (format == SendFormat.Hex)
        {
            data = SerialDataFormat.ParseHex(payload);
            preview = SerialDataFormat.FormatHex(data);
            serial.SendBytes(data);
        }
        else
        {
            serial.SendText(payload, lineEnding);
            var suffix = lineEnding switch
            {
                LineEnding.Cr => "\r",
                LineEnding.Lf => "\n",
                LineEnding.CrLf => "\r\n",
                _ => string.Empty,
            };
            data = encoding.Encode(payload + suffix);
            preview = payload;
        }

        traffic?.AddTx(data.Length);
        return (data, preview);
    }
}
