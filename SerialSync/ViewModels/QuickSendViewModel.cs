using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SerialSync.Dock;
using SerialSync.Models;
using SerialSync.Services;
using System.Collections.ObjectModel;

namespace SerialSync.ViewModels;

public partial class QuickSendViewModel : DockTabViewModel
{
    public override string Id => "quick-send";
    public override string Header => "快捷";
    public override DockSlot DefaultSlot => DockSlot.CenterBottom;

    private readonly ISerialPortService _serial;
    private readonly INotificationService _notifications;
    private readonly SendPresetStore _presetStore;
    private readonly SendHistoryService _history;
    private readonly SerialTrafficService _traffic;
    private readonly TextEncodingService _encoding;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private SendFormat _sendFormat = SendFormat.Text;

    [ObservableProperty]
    private LineEnding _lineEnding = LineEnding.None;

    [ObservableProperty]
    private QuickCommand? _selectedQuickCommand;

    [ObservableProperty]
    private string _quickName = string.Empty;

    public ObservableCollection<QuickCommand> QuickCommands { get; } = new();

    public Array SendFormats => Enum.GetValues<SendFormat>();
    public Array LineEndings => Enum.GetValues<LineEnding>();
    public bool IsTextMode => SendFormat == SendFormat.Text;
    public bool IsEditingQuick => SelectedQuickCommand is not null;
    public string SaveButtonText => IsEditingQuick ? "更新快捷" : "保存快捷";

    partial void OnSendFormatChanged(SendFormat value) => OnPropertyChanged(nameof(IsTextMode));
    partial void OnSelectedQuickCommandChanged(QuickCommand? value)
    {
        OnPropertyChanged(nameof(IsEditingQuick));
        OnPropertyChanged(nameof(SaveButtonText));
        if (value is null)
            return;

        InputText = value.Payload;
        SendFormat = value.Format;
        LineEnding = value.LineEnding;
        QuickName = value.Name;
    }

    public QuickSendViewModel(
        ISerialPortService serial,
        INotificationService notifications,
        SendPresetStore presetStore,
        SendHistoryService history,
        SerialTrafficService traffic,
        TextEncodingService encoding)
    {
        _serial = serial;
        _notifications = notifications;
        _presetStore = presetStore;
        _history = history;
        _traffic = traffic;
        _encoding = encoding;

        foreach (var cmd in _presetStore.Load().QuickCommands)
            QuickCommands.Add(cmd);

        _presetStore.PresetsChanged += (_, _) => ReloadFromStore();
    }

    private void ReloadFromStore()
    {
        QuickCommands.Clear();
        foreach (var cmd in _presetStore.Load().QuickCommands)
            QuickCommands.Add(cmd);
    }

    [RelayCommand]
    private async Task RunQuickCommand(QuickCommand? command)
    {
        if (command is null)
            return;

        if (!_serial.IsOpen)
        {
            _notifications.ShowWarning("快捷发送失败", "串口未连接");
            return;
        }

        try
        {
            var (payload, preview) = await Task.Run(() =>
                SerialSendOperations.Send(_serial, command.Payload, command.Format, command.LineEnding, _encoding, _traffic));
            _history.Add(preview, command.Payload, command.Format, command.LineEnding, payload.Length);
            _notifications.ShowSuccess("快捷发送", command.Name);
        }
        catch (Exception ex)
        {
            _notifications.ShowError("快捷发送失败", ex.Message);
        }
    }

    [RelayCommand]
    private void DeleteQuickCommand(QuickCommand? command)
    {
        if (command is null)
            return;

        QuickCommands.Remove(command);
        if (SelectedQuickCommand == command)
            SelectedQuickCommand = null;
        Persist();
    }

    [RelayCommand]
    private void SaveQuickCommand()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            _notifications.ShowWarning("无法保存", "请输入快捷指令内容");
            return;
        }

        var name = string.IsNullOrWhiteSpace(QuickName)
            ? BuildAutoName(InputText)
            : QuickName.Trim();

        if (SelectedQuickCommand is not null)
        {
            SelectedQuickCommand.Name = name;
            SelectedQuickCommand.Payload = InputText;
            SelectedQuickCommand.Format = SendFormat;
            SelectedQuickCommand.LineEnding = LineEnding;
            Persist();
            _notifications.ShowSuccess("已更新", name);
            return;
        }

        QuickCommands.Add(new QuickCommand
        {
            Name = name,
            Payload = InputText,
            Format = SendFormat,
            LineEnding = LineEnding,
        });
        Persist();
        _notifications.ShowSuccess("已保存快捷指令", name);
    }

    [RelayCommand]
    private void NewQuickCommand()
    {
        SelectedQuickCommand = null;
        InputText = string.Empty;
        QuickName = string.Empty;
    }

    [RelayCommand]
    private async Task ExportPresets(IStorageProvider? storageProvider)
    {
        if (storageProvider is null)
            return;

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出预设",
            SuggestedFileName = "serialsync-presets.json",
            FileTypeChoices = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }],
        });
        if (file is null)
            return;

        var storage = _presetStore.Load();
        var json = _presetStore.ToJson(storage);
        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(json));
        _notifications.ShowSuccess("已导出", file.Name);
    }

    [RelayCommand]
    private async Task ImportPresets(IStorageProvider? storageProvider)
    {
        if (storageProvider is null)
            return;

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "导入预设",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }],
        });
        if (files.Count == 0)
            return;

        await using var stream = await files[0].OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var imported = _presetStore.FromJson(System.Text.Encoding.UTF8.GetString(ms.ToArray()));
        if (imported is null)
        {
            _notifications.ShowError("导入失败", "文件格式无效");
            return;
        }

        _presetStore.Save(imported);
        _notifications.ShowSuccess("已导入", files[0].Name);
    }

    private static string BuildAutoName(string input)
    {
        var preview = input.Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (preview.Length > 24)
            preview = preview[..24] + "…";
        return string.IsNullOrWhiteSpace(preview) ? "快捷指令" : preview;
    }

    private void Persist()
    {
        var storage = _presetStore.Load();
        storage.QuickCommands = QuickCommands.ToList();
        _presetStore.Save(storage);
    }
}
