using System;
using System.ComponentModel;
using RoosterControl.Integrations.StardewUi.Compat;
using RoosterControl.Integrations.StardewUi.Produce;

namespace RoosterControl.Integrations.StardewUi;

internal sealed class RoosterEditorHubViewModel : INotifyPropertyChanged
{
    private RoosterEditorHubTab _selectedTab;

    public event PropertyChangedEventHandler? PropertyChanged;

    public RoosterEditorHubViewModel(
        RoosterCompatEditorViewModel compat,
        RoosterProduceEditorViewModel produce,
        RoosterSettingsEditorViewModel settings,
        RoosterDebugEditorViewModel debug,
        RoosterEditorHubTab initialTab)
    {
        Compat = compat ?? throw new ArgumentNullException(nameof(compat));
        Produce = produce ?? throw new ArgumentNullException(nameof(produce));
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        Debug = debug ?? throw new ArgumentNullException(nameof(debug));
        _selectedTab = initialTab;
        Compat.PropertyChanged += OnChildPropertyChanged;
        Produce.PropertyChanged += OnChildPropertyChanged;
        Settings.PropertyChanged += OnChildPropertyChanged;
        Debug.PropertyChanged += OnChildPropertyChanged;
    }

    public string Title => "RoosterControl Editor";

    public RoosterCompatEditorViewModel Compat { get; }

    public RoosterProduceEditorViewModel Produce { get; }

    public RoosterSettingsEditorViewModel Settings { get; }

    public RoosterDebugEditorViewModel Debug { get; }

    public bool IsCompatTab => _selectedTab == RoosterEditorHubTab.Compat;

    public bool IsProduceTab => _selectedTab == RoosterEditorHubTab.Produce;

    public bool IsSettingsTab => _selectedTab == RoosterEditorHubTab.Settings;

    public bool IsDebugTab => _selectedTab == RoosterEditorHubTab.Debug;

    public bool HasDebugTab =>
#if RCTRL_DEV
        true;
#else
        false;
#endif

    public string CompatTabText => FormatTabText("Compat", IsCompatTab, Compat.CanSave);

    public string ProduceTabText => FormatTabText("Produce", IsProduceTab, Produce.CanSave);

    public string SettingsTabText => FormatTabText("Settings", IsSettingsTab, Settings.CanSave);

    public string DebugTabText => FormatTabText("Debug", IsDebugTab, Debug.CanSave);

    public void ShowCompatTab() => SetTab(RoosterEditorHubTab.Compat);

    public void ShowProduceTab() => SetTab(RoosterEditorHubTab.Produce);

    public void ShowSettingsTab() => SetTab(RoosterEditorHubTab.Settings);

    public void ShowDebugTab() => SetTab(RoosterEditorHubTab.Debug);

    public bool CanCloseMenu()
        => !Compat.CanSave
            && !Produce.CanSave
            && !Settings.CanSave
            && !Debug.CanSave
            ;

    private void SetTab(RoosterEditorHubTab tab)
    {
        if (_selectedTab == tab)
            return;

        _selectedTab = tab;
        OnPropertyChanged(nameof(IsCompatTab));
        OnPropertyChanged(nameof(IsProduceTab));
        OnPropertyChanged(nameof(IsSettingsTab));
        OnPropertyChanged(nameof(IsDebugTab));
        OnPropertyChanged(nameof(CompatTabText));
        OnPropertyChanged(nameof(ProduceTabText));
        OnPropertyChanged(nameof(SettingsTabText));
        OnPropertyChanged(nameof(DebugTabText));
    }

    private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.Equals(e.PropertyName, nameof(RoosterCompatEditorViewModel.CanSave), StringComparison.Ordinal)
            && !string.Equals(e.PropertyName, nameof(RoosterProduceEditorViewModel.CanSave), StringComparison.Ordinal))
        {
            if (!string.Equals(e.PropertyName, nameof(RoosterSettingsEditorViewModel.CanSave), StringComparison.Ordinal)
                && !string.Equals(e.PropertyName, nameof(RoosterDebugEditorViewModel.CanSave), StringComparison.Ordinal)
                )
                return;
        }

        OnPropertyChanged(nameof(CompatTabText));
        OnPropertyChanged(nameof(ProduceTabText));
        OnPropertyChanged(nameof(SettingsTabText));
        OnPropertyChanged(nameof(DebugTabText));
    }

    private static string FormatTabText(string label, bool isSelected, bool hasChanges)
    {
        var text = hasChanges ? $"{label} *" : label;
        return isSelected ? $"[{text}]" : text;
    }

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

internal enum RoosterEditorHubTab
{
    Compat,
    Produce,
    Settings,
    Debug,
}
