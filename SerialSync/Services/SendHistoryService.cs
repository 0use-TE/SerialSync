using SerialSync.Models;
using System.Collections.ObjectModel;

namespace SerialSync.Services;

public sealed class SendHistoryService
{
    public ObservableCollection<SendRecord> Items { get; } = new();

    public void Add(
        string preview,
        string payload,
        SendFormat format,
        LineEnding lineEnding,
        int byteCount,
        bool isFile = false,
        string? filePath = null)
    {
        Items.Insert(0, new SendRecord
        {
            Preview = preview,
            Payload = payload,
            Format = format,
            LineEnding = lineEnding,
            ByteCount = byteCount,
            Time = DateTime.Now.TimeOfDay,
            IsFile = isFile,
            FilePath = filePath,
        });

        while (Items.Count > 200)
            Items.RemoveAt(Items.Count - 1);
    }
}
