using SerialSync.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerialSync.Services;

public sealed class SerialPortSettingsStore
{
    private const string FileName = "serial-settings.json";
    private readonly string _path;

    public SerialPortSettingsStore()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SerialSync");
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, FileName);
    }

    public SerialPortSettingsStorage Load()
    {
        if (!File.Exists(_path))
            return new SerialPortSettingsStorage();

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize(json, SerialPortSettingsJsonContext.Default.SerialPortSettingsStorage)
                ?? new SerialPortSettingsStorage();
        }
        catch
        {
            return new SerialPortSettingsStorage();
        }
    }

    public void Save(SerialPortSettingsStorage settings)
    {
        var json = JsonSerializer.Serialize(settings, SerialPortSettingsJsonContext.Default.SerialPortSettingsStorage);
        File.WriteAllText(_path, json);
    }
}

[JsonSerializable(typeof(SerialPortSettingsStorage))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class SerialPortSettingsJsonContext : JsonSerializerContext;
