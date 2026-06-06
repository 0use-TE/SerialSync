namespace SerialSync.Services;

public static class ModbusCrc
{
    public static ushort Crc16(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
                crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
        }

        return crc;
    }

    public static byte[] AppendCrc(ReadOnlySpan<byte> payload)
    {
        var crc = Crc16(payload);
        var result = new byte[payload.Length + 2];
        payload.CopyTo(result);
        result[^2] = (byte)(crc & 0xFF);
        result[^1] = (byte)(crc >> 8);
        return result;
    }

    public static string FormatHex(ReadOnlySpan<byte> data) => SerialDataFormat.FormatHex(data);
}
