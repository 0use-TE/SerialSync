using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Crystal.Avalonia;
using GOZA.Dock.Controls;
using SerialSync.Dock;
using SerialSync.Models;
using SerialSync.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SerialSync.ViewModels;

public partial class MainViewModel : ObservableObject, ILifecycleAware
{
    public ObservableCollection<DockTabViewModel> LeftTabs { get; } = new();
    public ObservableCollection<DockTabViewModel> CenterTopTabs { get; } = new();
    public ObservableCollection<DockTabViewModel> CenterBottomTabs { get; } = new();
    public ObservableCollection<DockTabViewModel> RightTabs { get; } = new();

    [ObservableProperty]
    private DockTabViewModel? _leftSelected;

    [ObservableProperty]
    private DockTabViewModel? _centerTopSelected;

    [ObservableProperty]
    private DockTabViewModel? _centerBottomSelected;

    [ObservableProperty]
    private DockTabViewModel? _rightSelected;

    [ObservableProperty]
    private GridLength _leftGridLength = new(220);

    [ObservableProperty]
    private GridLength _rightGridLength = new(280);

    [ObservableProperty]
    private GridLength _centerTopGridLength = new(1.375, GridUnitType.Star);

    [ObservableProperty]
    private GridLength _centerBottomGridLength = new(1, GridUnitType.Star);

    [ObservableProperty]
    private GridLength _centerColumnWidth = new(1, GridUnitType.Star);

    [ObservableProperty]
    private GridLength _leftSplitterColumnWidth = new(6);

    [ObservableProperty]
    private GridLength _rightSplitterColumnWidth = new(6);

    [ObservableProperty]
    private GridLength _centerHSplitterRowHeight = new(6);

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private bool _isTutorialOpen = true;

    public SerialPortSettingsViewModel SerialSettings { get; }
    public ReceiveViewModel Receive { get; }
    public SendViewModel Send { get; }
    public QuickSendViewModel QuickSend { get; }
    public SequenceViewModel Sequence { get; }
    public LogViewModel Log { get; }
    public SerialTrafficService Traffic { get; }

    private readonly LayoutStore _layoutStore;
    private readonly Dictionary<string, DockTabViewModel> _tabLookup;
    private bool _suspendSave;

    public MainViewModel(
        SerialPortSettingsViewModel serialSettings,
        ReceiveViewModel receive,
        SendViewModel send,
        QuickSendViewModel quickSend,
        SequenceViewModel sequence,
        LogViewModel log,
        LayoutStore layoutStore,
        SerialTrafficService traffic)
    {
        _layoutStore = layoutStore;
        Traffic = traffic;
        SerialSettings = serialSettings;
        Receive = receive;
        Send = send;
        QuickSend = quickSend;
        Sequence = sequence;
        Log = log;

        _tabLookup = new Dictionary<string, DockTabViewModel>(StringComparer.Ordinal)
        {
            [serialSettings.Id] = serialSettings,
            [receive.Id] = receive,
            [send.Id] = send,
            [quickSend.Id] = quickSend,
            [sequence.Id] = sequence,
            [log.Id] = log,
        };

        _suspendSave = true;
        var stored = _layoutStore.Load();
        ApplyTabLayout(stored);
        IsDarkMode = stored.IsDarkMode;
        IsTutorialOpen = stored.IsTutorialOpen;

        SubscribeTabLayout(LeftTabs);
        SubscribeTabLayout(CenterTopTabs);
        SubscribeTabLayout(CenterBottomTabs);
        SubscribeTabLayout(RightTabs);

        SelectStoredTab(stored);
        _suspendSave = false;
    }

    public Task OnLoadedAsync()
    {
        _suspendSave = true;
        var stored = _layoutStore.Load();
        LeftGridLength = new GridLength(stored.LeftWidth);
        RightGridLength = new GridLength(stored.RightWidth);
        CenterTopGridLength = new GridLength(stored.CenterTopBottomProportion, GridUnitType.Star);
        CenterBottomGridLength = new GridLength(1, GridUnitType.Star);
        IsDarkMode = stored.IsDarkMode;

        if (Application.Current is not null)
            Application.Current.RequestedThemeVariant = IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;

        _suspendSave = false;
        return Task.CompletedTask;
    }

    public Task OnUnloaded()
    {
        SaveLayout();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        if (Application.Current is not null)
            Application.Current.RequestedThemeVariant = IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
        SaveLayout();
    }

    [RelayCommand]
    private void OpenTutorial() => IsTutorialOpen = true;

    [RelayCommand]
    private void CloseTutorial()
    {
        IsTutorialOpen = false;
        SaveLayout();
    }

    [RelayCommand]
    private void ToggleTutorial() => IsTutorialOpen = !IsTutorialOpen;

    partial void OnLeftGridLengthChanged(GridLength value) => SaveLayout();
    partial void OnRightGridLengthChanged(GridLength value) => SaveLayout();
    partial void OnCenterTopGridLengthChanged(GridLength value) => SaveLayout();
    partial void OnCenterBottomGridLengthChanged(GridLength value) => SaveLayout();
    partial void OnIsDarkModeChanged(bool value) => SaveLayout();
    partial void OnIsTutorialOpenChanged(bool value) => SaveLayout();
    partial void OnLeftSelectedChanged(DockTabViewModel? value) => SaveLayout();
    partial void OnCenterTopSelectedChanged(DockTabViewModel? value) => SaveLayout();
    partial void OnCenterBottomSelectedChanged(DockTabViewModel? value) => SaveLayout();
    partial void OnRightSelectedChanged(DockTabViewModel? value) => SaveLayout();

    private void ApplyTabLayout(LayoutStorage stored)
    {
        var assigned = new HashSet<string>(StringComparer.Ordinal);
        RebuildTabs(LeftTabs, stored.LeftItems, assigned);
        RebuildTabs(RightTabs, stored.RightItems, assigned);
        RebuildTabs(CenterTopTabs, stored.CenterTopItems, assigned);
        RebuildTabs(CenterBottomTabs, stored.CenterBottomItems, assigned);

        foreach (var tab in _tabLookup.Values.Where(t => !assigned.Contains(t.Id)))
            GetCollectionForSlot(tab.DefaultSlot).Add(tab);
    }

    private void RebuildTabs(ObservableCollection<DockTabViewModel> target, IList<string>? ids, ISet<string> assigned)
    {
        target.Clear();
        if (ids is null)
            return;

        foreach (var id in ids)
        {
            if (string.IsNullOrWhiteSpace(id) || assigned.Contains(id))
                continue;

            if (_tabLookup.TryGetValue(id, out var tab))
            {
                target.Add(tab);
                assigned.Add(id);
            }
        }
    }

    private ObservableCollection<DockTabViewModel> GetCollectionForSlot(DockSlot slot) =>
        slot switch
        {
            DockSlot.Left => LeftTabs,
            DockSlot.CenterTop => CenterTopTabs,
            DockSlot.CenterBottom => CenterBottomTabs,
            DockSlot.Right => RightTabs,
            _ => CenterBottomTabs,
        };

    private void SelectStoredTab(LayoutStorage stored)
    {
        LeftSelected = FindTab(LeftTabs, stored.LeftSelectedId);
        CenterTopSelected = FindTab(CenterTopTabs, stored.CenterTopSelectedId);
        CenterBottomSelected = FindTab(CenterBottomTabs, stored.CenterBottomSelectedId);
        RightSelected = FindTab(RightTabs, stored.RightSelectedId);

        EnsureSelection(LeftTabs, () => LeftSelected, v => LeftSelected = v);
        EnsureSelection(CenterTopTabs, () => CenterTopSelected, v => CenterTopSelected = v);
        EnsureSelection(CenterBottomTabs, () => CenterBottomSelected, v => CenterBottomSelected = v);
        EnsureSelection(RightTabs, () => RightSelected, v => RightSelected = v);
    }

    private static DockTabViewModel? FindTab(ObservableCollection<DockTabViewModel> tabs, string? id) =>
        string.IsNullOrWhiteSpace(id) ? null : tabs.FirstOrDefault(t => t.Id == id);

    private static void EnsureSelection(
        ObservableCollection<DockTabViewModel> tabs,
        Func<DockTabViewModel?> getSelected,
        Action<DockTabViewModel?> setSelected)
    {
        if (tabs.Count > 0 && (getSelected() is null || !tabs.Contains(getSelected()!)))
            setSelected(tabs[0]);
    }

    private void SubscribeTabLayout(ObservableCollection<DockTabViewModel> tabs) =>
        tabs.CollectionChanged += OnTabLayoutChanged;

    private void OnTabLayoutChanged(object? sender, NotifyCollectionChangedEventArgs e) => SaveLayout();

    private void SaveLayout()
    {
        if (_suspendSave)
            return;

        var bottom = CenterBottomGridLength.Value;
        var proportion = bottom <= 0 ? 1.375 : CenterTopGridLength.Value / bottom;

        _layoutStore.Save(new LayoutStorage
        {
            LeftWidth = LeftGridLength.Value,
            RightWidth = RightGridLength.Value,
            CenterTopBottomProportion = proportion,
            IsDarkMode = IsDarkMode,
            IsTutorialOpen = IsTutorialOpen,
            LeftItems = LeftTabs.Select(t => t.Id).ToList(),
            RightItems = RightTabs.Select(t => t.Id).ToList(),
            CenterTopItems = CenterTopTabs.Select(t => t.Id).ToList(),
            CenterBottomItems = CenterBottomTabs.Select(t => t.Id).ToList(),
            LeftSelectedId = LeftSelected?.Id,
            CenterTopSelectedId = CenterTopSelected?.Id,
            CenterBottomSelectedId = CenterBottomSelected?.Id,
            RightSelectedId = RightSelected?.Id,
        });
    }
}
