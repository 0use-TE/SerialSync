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
    private readonly SendViewModel _send;

    public ObservableCollection<QuickCommand> QuickCommands { get; } = new();

    public QuickSendViewModel(
        ISerialPortService serial,
        INotificationService notifications,
        SendPresetStore presetStore,
        SendHistoryService history,
        SerialTrafficService traffic,
        SendViewModel send)
    {
        _serial = serial;
        _notifications = notifications;
        _presetStore = presetStore;
        _history = history;
        _traffic = traffic;
        _send = send;

        foreach (var cmd in _presetStore.Load().QuickCommands)
            QuickCommands.Add(cmd);
    }

    [RelayCommand]
    private async Task RunQuickCommand(QuickCommand? command)
    {
        if (command is null)
            return;

        try
        {
            var (payload, preview) = await Task.Run(() =>
                SerialSendOperations.Send(_serial, command.Payload, command.Format, command.LineEnding, _traffic));
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
        Persist();
    }

    [RelayCommand]
    private void SaveFromSendPanel()
    {
        if (string.IsNullOrWhiteSpace(_send.InputText))
        {
            _notifications.ShowWarning("无法保存", "请先在「发送」面板输入内容");
            return;
        }

        var preview = _send.InputText.Replace('\r', ' ').Replace('\n', ' ').Trim();
        var name = preview.Length > 24 ? preview[..24] + "…" : preview;
        if (string.IsNullOrWhiteSpace(name))
            name = _send.SendFormat == SendFormat.Hex ? "HEX 指令" : "快捷指令";

        QuickCommands.Add(new QuickCommand
        {
            Name = name,
            Payload = _send.InputText,
            Format = _send.SendFormat,
            LineEnding = _send.LineEnding,
        });
        Persist();
        _notifications.ShowSuccess("已保存快捷指令", name);
    }

    private void Persist()
    {
        var storage = _presetStore.Load();
        storage.QuickCommands = QuickCommands.ToList();
        _presetStore.Save(storage);
    }
}
