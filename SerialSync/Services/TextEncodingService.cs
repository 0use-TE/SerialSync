using CommunityToolkit.Mvvm.ComponentModel;
using System.Text;

namespace SerialSync.Services;

public enum TextEncodingKind
{
    Utf8,
    Ascii,
    Gbk,
}

public partial class TextEncodingService : ObservableObject
{
    [ObservableProperty]
    private TextEncodingKind _kind = TextEncodingKind.Utf8;

    public Encoding GetEncoding() => Kind switch
    {
        TextEncodingKind.Ascii => Encoding.ASCII,
        TextEncodingKind.Gbk => Encoding.GetEncoding("GBK"),
        _ => Encoding.UTF8,
    };

    public string Decode(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return string.Empty;

        return GetEncoding().GetString(data);
    }

    public byte[] Encode(string text) => GetEncoding().GetBytes(text);
}
