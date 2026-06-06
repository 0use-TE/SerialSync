using SerialSync.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerialSync.Services;

public sealed class SendHistoryStore
{
    private const string FileName = "send-history.json";
    private readonly string _path;

    public SendHistoryStore()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SerialSync");
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, FileName);
    }

    public List<SendRecordDto> Load()
    {
        if (!File.Exists(_path))
            return [];

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize(json, SendHistoryJsonContext.Default.ListSendRecordDto) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void Save(IReadOnlyList<SendRecordDto> records)
    {
        var json = JsonSerializer.Serialize(records, SendHistoryJsonContext.Default.ListSendRecordDto);
        File.WriteAllText(_path, json);
    }
}

public sealed class SendRecordDto
{
    public string Preview { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public SendFormat Format { get; set; }
    public LineEnding LineEnding { get; set; }
    public int ByteCount { get; set; }
    public long TimeTicks { get; set; }
    public bool IsFile { get; set; }
    public string? FilePath { get; set; }

    public static SendRecordDto From(SendRecord r) => new()
    {
        Preview = r.Preview,
        Payload = r.Payload,
        Format = r.Format,
        LineEnding = r.LineEnding,
        ByteCount = r.ByteCount,
        TimeTicks = r.Time.Ticks,
        IsFile = r.IsFile,
        FilePath = r.FilePath,
    };

    public SendRecord ToRecord() => new()
    {
        Preview = Preview,
        Payload = Payload,
        Format = Format,
        LineEnding = LineEnding,
        ByteCount = ByteCount,
        Time = TimeSpan.FromTicks(TimeTicks),
        IsFile = IsFile,
        FilePath = FilePath,
    };
}

[JsonSerializable(typeof(List<SendRecordDto>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class SendHistoryJsonContext : JsonSerializerContext;
