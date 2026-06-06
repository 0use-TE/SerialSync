using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SerialSync.Dock;
using SerialSync.Models;
using SerialSync.Services;
using System.Collections.ObjectModel;

namespace SerialSync.ViewModels;

public partial class SequenceViewModel : DockTabViewModel
{
    public override string Id => "sequence";
    public override string Header => "序列";
    public override DockSlot DefaultSlot => DockSlot.CenterBottom;

    private readonly ISerialPortService _serial;
    private readonly INotificationService _notifications;
    private readonly SendPresetStore _presetStore;
    private readonly SendHistoryService _history;
    private readonly SerialTrafficService _traffic;
    private readonly SendViewModel _send;
    private CancellationTokenSource? _sequenceCts;

    [ObservableProperty]
    private SendSequence? _selectedSequence;

    [ObservableProperty]
    private int _stepDelayMs = 100;

    [ObservableProperty]
    private bool _isRunningSequence;

    public ObservableCollection<SendSequence> Sequences { get; } = new();
    public bool HasSelectedSequence => SelectedSequence is not null;

    partial void OnSelectedSequenceChanged(SendSequence? value) => OnPropertyChanged(nameof(HasSelectedSequence));

    public SequenceViewModel(
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

        foreach (var seq in _presetStore.Load().Sequences)
            Sequences.Add(seq);
    }

    [RelayCommand]
    private void CreateSequence()
    {
        var seq = new SendSequence { Name = $"序列 {Sequences.Count + 1}" };
        Sequences.Add(seq);
        SelectedSequence = seq;
        Persist();
    }

    [RelayCommand]
    private void AddStepFromSendPanel()
    {
        if (SelectedSequence is null)
        {
            _notifications.ShowWarning("无法添加", "请先选择或新建序列");
            return;
        }

        if (string.IsNullOrWhiteSpace(_send.InputText))
        {
            _notifications.ShowWarning("无法添加", "请先在「发送」面板输入内容");
            return;
        }

        SelectedSequence.Steps.Add(new SendSequenceStep
        {
            Payload = _send.InputText,
            Format = _send.SendFormat,
            LineEnding = _send.LineEnding,
            DelayAfterMs = StepDelayMs,
        });
        Persist();
        OnPropertyChanged(nameof(SelectedSequence));
        _notifications.ShowSuccess("已添加步骤", $"共 {SelectedSequence.Steps.Count} 步");
    }

    [RelayCommand]
    private void RemoveSequenceStep(SendSequenceStep? step)
    {
        if (SelectedSequence is null || step is null)
            return;

        SelectedSequence.Steps.Remove(step);
        Persist();
        OnPropertyChanged(nameof(SelectedSequence));
    }

    [RelayCommand]
    private void DeleteSequence(SendSequence? sequence)
    {
        if (sequence is null)
            return;

        Sequences.Remove(sequence);
        if (SelectedSequence == sequence)
            SelectedSequence = Sequences.FirstOrDefault();
        Persist();
    }

    [RelayCommand]
    private async Task RunSequence(SendSequence? sequence)
    {
        if (sequence is null || sequence.Steps.Count == 0)
        {
            _notifications.ShowWarning("无法运行", "序列为空");
            return;
        }

        if (!_serial.IsOpen)
        {
            _notifications.ShowError("序列运行失败", "串口未连接");
            return;
        }

        _sequenceCts?.Cancel();
        _sequenceCts = new CancellationTokenSource();
        var token = _sequenceCts.Token;

        IsRunningSequence = true;
        try
        {
            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                var step = sequence.Steps[i];
                var (payload, preview) = await Task.Run(() =>
                    SerialSendOperations.Send(_serial, step.Payload, step.Format, step.LineEnding, _traffic));
                _history.Add(preview, step.Payload, step.Format, step.LineEnding, payload.Length);

                if (step.DelayAfterMs > 0 && i < sequence.Steps.Count - 1)
                    await Task.Delay(step.DelayAfterMs, token);
            }

            _notifications.ShowSuccess("序列完成", $"{sequence.Name} · {sequence.Steps.Count} 步");
        }
        catch (OperationCanceledException)
        {
            _notifications.ShowInfo("序列已停止", sequence.Name);
        }
        catch (Exception ex)
        {
            _notifications.ShowError("序列失败", ex.Message);
        }
        finally
        {
            IsRunningSequence = false;
        }
    }

    [RelayCommand]
    private void StopSequence() => _sequenceCts?.Cancel();

    private void Persist()
    {
        var storage = _presetStore.Load();
        storage.Sequences = Sequences.ToList();
        _presetStore.Save(storage);
    }
}
