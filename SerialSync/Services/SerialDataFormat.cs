using System.Text;
using System.Text.RegularExpressions;

namespace SerialSync.Services;

public static partial class SerialDataFormat
{
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
            if (!HexBytePattern().IsMatch(parts[i]))
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

    [GeneratedRegex("^[0-9A-Fa-f]{1,2}$")]
    private static partial Regex HexBytePattern();
}
