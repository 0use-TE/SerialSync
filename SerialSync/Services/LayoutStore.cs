using SerialSync.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerialSync.Services;

public sealed class LayoutStore
{
    private const string FileName = "layout.json";
    private readonly string _path;

    public LayoutStore()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SerialSync");
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, FileName);
    }

    public string LogFolder =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SerialSync",
            "Logs");

    public LayoutStorage Load()
    {
        if (!File.Exists(_path))
            return new LayoutStorage();

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize(json, AppJsonContext.Default.LayoutStorage)
                ?? new LayoutStorage();
        }
        catch
        {
            return new LayoutStorage();
        }
    }

    public void Save(LayoutStorage layout)
    {
        var json = JsonSerializer.Serialize(layout, AppJsonContext.Default.LayoutStorage);
        File.WriteAllText(_path, json);
    }
}

[JsonSerializable(typeof(LayoutStorage))]
[JsonSerializable(typeof(List<string>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class AppJsonContext : JsonSerializerContext;
