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
    private readonly TextEncodingService _encoding;
    private CancellationTokenSource? _sequenceCts;

    [ObservableProperty]
    private SendSequence? _selectedSequence;

    [ObservableProperty]
    private SendSequenceStep? _selectedStep;

    [ObservableProperty]
    private int _stepDelayMs = 100;

    [ObservableProperty]
    private bool _isRunningSequence;

    [ObservableProperty]
    private bool _loopSequence;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private SendFormat _sendFormat = SendFormat.Text;

    [ObservableProperty]
    private LineEnding _lineEnding = LineEnding.None;

    public ObservableCollection<SendSequence> Sequences { get; } = new();
    public bool HasSelectedSequence => SelectedSequence is not null;
    public bool IsEditingStep => SelectedStep is not null;
    public string StepButtonText => IsEditingStep ? "更新步骤" : "添加步骤";

    public Array SendFormats => Enum.GetValues<SendFormat>();
    public Array LineEndings => Enum.GetValues<LineEnding>();
    public bool IsTextMode => SendFormat == SendFormat.Text;

    partial void OnSelectedSequenceChanged(SendSequence? value)
    {
        OnPropertyChanged(nameof(HasSelectedSequence));
        SelectedStep = null;
    }

    partial void OnSendFormatChanged(SendFormat value) => OnPropertyChanged(nameof(IsTextMode));

    public SequenceViewModel(
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

        foreach (var seq in _presetStore.Load().Sequences)
        {
            NormalizeSequenceSteps(seq);
            Sequences.Add(seq);
        }

        _presetStore.PresetsChanged += (_, _) => ReloadFromStore();
    }

    private void ReloadFromStore()
    {
        Sequences.Clear();
        foreach (var seq in _presetStore.Load().Sequences)
        {
            NormalizeSequenceSteps(seq);
            Sequences.Add(seq);
        }

        SelectedSequence = Sequences.FirstOrDefault(s => s.Id == SelectedSequence?.Id) ?? Sequences.FirstOrDefault();
    }

    partial void OnSelectedStepChanged(SendSequenceStep? value)
    {
        OnPropertyChanged(nameof(IsEditingStep));
        OnPropertyChanged(nameof(StepButtonText));
        if (value is null)
            return;

        InputText = value.Payload;
        SendFormat = value.Format;
        LineEnding = value.LineEnding;
        StepDelayMs = value.DelayAfterMs;
    }

    [RelayCommand]
    private void CommitSequenceName() => Persist();

    private static void NormalizeSequenceSteps(SendSequence seq)
    {
        if (seq.Steps is ObservableCollection<SendSequenceStep>)
            return;

        seq.Steps = new ObservableCollection<SendSequenceStep>(seq.Steps.ToList());
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
    private void AddOrUpdateStep()
    {
        if (SelectedSequence is null)
        {
            _notifications.ShowWarning("无法添加", "请先选择或新建序列");
            return;
        }

        if (string.IsNullOrWhiteSpace(InputText))
        {
            _notifications.ShowWarning("无法添加", "请输入步骤内容");
            return;
        }

        if (SelectedStep is not null)
        {
            SelectedStep.Payload = InputText;
            SelectedStep.Format = SendFormat;
            SelectedStep.LineEnding = LineEnding;
            SelectedStep.DelayAfterMs = StepDelayMs;
            Persist();
            _notifications.ShowSuccess("已更新步骤", SelectedSequence.Name);
            return;
        }

        SelectedSequence.Steps.Add(new SendSequenceStep
        {
            Payload = InputText,
            Format = SendFormat,
            LineEnding = LineEnding,
            DelayAfterMs = StepDelayMs,
        });
        Persist();
        _notifications.ShowSuccess("已添加步骤", $"共 {SelectedSequence.StepCount} 步");
    }

    [RelayCommand]
    private void ClearStepSelection() => SelectedStep = null;

    [RelayCommand]
    private void RemoveSequenceStep(SendSequenceStep? step)
    {
        if (SelectedSequence is null || step is null)
            return;

        SelectedSequence.Steps.Remove(step);
        if (SelectedStep == step)
            SelectedStep = null;
        Persist();
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
            do
            {
                for (var i = 0; i < sequence.Steps.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    var step = sequence.Steps[i];
                    var (payload, preview) = await Task.Run(() =>
                        SerialSendOperations.Send(_serial, step.Payload, step.Format, step.LineEnding, _encoding, _traffic));
                    _history.Add(preview, step.Payload, step.Format, step.LineEnding, payload.Length);

                    if (step.DelayAfterMs > 0 && i < sequence.Steps.Count - 1)
                        await Task.Delay(step.DelayAfterMs, token);
                }
            } while (LoopSequence && !token.IsCancellationRequested);

            _notifications.ShowSuccess(LoopSequence ? "循环序列已停止" : "序列完成", $"{sequence.Name} · {sequence.Steps.Count} 步");
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
