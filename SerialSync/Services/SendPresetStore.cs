using SerialSync.Models;
using SerialSync.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerialSync.Services;

public sealed class SendPresetStore
{
    private const string FileName = "send-presets.json";
    private readonly string _path;

    public SendPresetStore()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SerialSync");
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, FileName);
    }

    public SendPresetStorage Load()
    {
        if (!File.Exists(_path))
            return new SendPresetStorage();

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize(json, SendPresetJsonContext.Default.SendPresetStorage)
                ?? new SendPresetStorage();
        }
        catch
        {
            return new SendPresetStorage();
        }
    }

    public void Save(SendPresetStorage storage)
    {
        var json = JsonSerializer.Serialize(storage, SendPresetJsonContext.Default.SendPresetStorage);
        File.WriteAllText(_path, json);
    }
}

[JsonSerializable(typeof(SendPresetStorage))]
[JsonSerializable(typeof(QuickCommand))]
[JsonSerializable(typeof(SendSequence))]
[JsonSerializable(typeof(SendSequenceStep))]
[JsonSerializable(typeof(List<QuickCommand>))]
[JsonSerializable(typeof(List<SendSequenceStep>))]
[JsonSerializable(typeof(List<SendSequence>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class SendPresetJsonContext : JsonSerializerContext;
