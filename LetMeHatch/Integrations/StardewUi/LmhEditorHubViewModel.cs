using System;
using System.ComponentModel;
using LetMeHatch.Integrations.StardewUi.Advanced;
using LetMeHatch.Integrations.StardewUi.Breeding;
using LetMeHatch.Integrations.StardewUi.Colors;

namespace LetMeHatch.Integrations.StardewUi;

public sealed class LmhEditorHubViewModel : INotifyPropertyChanged
{
    private readonly LmhEditorHubTab _initialTab;
    private LmhEditorHubTab _selectedTab;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LmhEditorHubViewModel(
        LmhBreedingEditorViewModel breeding,
        LmhColorsEditorViewModel colors,
        LmhItemMappingsEditorViewModel mappings,
        LmhSettingsEditorViewModel settings,
        LmhDebugEditorViewModel debug,
        LmhEditorHubTab initialTab)
    {
        Breeding = breeding ?? throw new ArgumentNullException(nameof(breeding));
        Colors = colors ?? throw new ArgumentNullException(nameof(colors));
        Mappings = mappings ?? throw new ArgumentNullException(nameof(mappings));
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        Debug = debug ?? throw new ArgumentNullException(nameof(debug));
        _initialTab = initialTab;
        _selectedTab = initialTab;
        Breeding.PropertyChanged += OnChildPropertyChanged;
        Colors.PropertyChanged += OnChildPropertyChanged;
        Mappings.PropertyChanged += OnChildPropertyChanged;
        Settings.PropertyChanged += OnChildPropertyChanged;
        Debug.PropertyChanged += OnChildPropertyChanged;
    }

    public string Title => "LetMeHatch Editor";

    public LmhBreedingEditorViewModel Breeding { get; }

    public LmhColorsEditorViewModel Colors { get; }

    public LmhItemMappingsEditorViewModel Mappings { get; }

    public LmhSettingsEditorViewModel Settings { get; }

    public LmhDebugEditorViewModel Debug { get; }

    public bool IsBreedingTab => _selectedTab == LmhEditorHubTab.Breeding;

    public bool IsColorsTab => _selectedTab == LmhEditorHubTab.Colors;

    public bool IsMappingsTab => _selectedTab == LmhEditorHubTab.Mappings;

    public bool IsSettingsTab => _selectedTab == LmhEditorHubTab.Settings;

    public bool IsDebugTab => _selectedTab == LmhEditorHubTab.Debug;

    public string BreedingTabText => FormatTabText("Breeding", IsBreedingTab, Breeding.CanSave);

    public string ColorsTabText => FormatTabText("Colors", IsColorsTab, Colors.CanSave);

    public string MappingsTabText => FormatTabText("Mappings", IsMappingsTab, Mappings.CanSave);

    public string SettingsTabText => FormatTabText("Settings", IsSettingsTab, Settings.CanSave);

    public string DebugTabText => FormatTabText("Debug", IsDebugTab, Debug.CanSave);

    public void ShowBreedingTab() => SetTab(LmhEditorHubTab.Breeding);

    public void ShowColorsTab() => SetTab(LmhEditorHubTab.Colors);

    public void ShowMappingsTab() => SetTab(LmhEditorHubTab.Mappings);

    public void ShowSettingsTab() => SetTab(LmhEditorHubTab.Settings);

    public void ShowDebugTab() => SetTab(LmhEditorHubTab.Debug);

    public bool CanCloseMenu()
        => !Breeding.CanSave
            && !Colors.CanSave
            && !Mappings.CanSave
            && !Settings.CanSave
            && !Debug.CanSave;

    private void SetTab(LmhEditorHubTab tab)
    {
        if (_selectedTab == tab)
            return;

        _selectedTab = tab;
        OnPropertyChanged(nameof(IsBreedingTab));
        OnPropertyChanged(nameof(IsColorsTab));
        OnPropertyChanged(nameof(IsMappingsTab));
        OnPropertyChanged(nameof(IsSettingsTab));
        OnPropertyChanged(nameof(IsDebugTab));
        OnPropertyChanged(nameof(BreedingTabText));
        OnPropertyChanged(nameof(ColorsTabText));
        OnPropertyChanged(nameof(MappingsTabText));
        OnPropertyChanged(nameof(SettingsTabText));
        OnPropertyChanged(nameof(DebugTabText));
    }

    private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.Equals(e.PropertyName, nameof(LmhBreedingEditorViewModel.CanSave), StringComparison.Ordinal)
            && !string.Equals(e.PropertyName, nameof(LmhColorsEditorViewModel.CanSave), StringComparison.Ordinal)
            && !string.Equals(e.PropertyName, nameof(LmhItemMappingsEditorViewModel.CanSave), StringComparison.Ordinal)
            && !string.Equals(e.PropertyName, nameof(LmhSettingsEditorViewModel.CanSave), StringComparison.Ordinal)
            && !string.Equals(e.PropertyName, nameof(LmhDebugEditorViewModel.CanSave), StringComparison.Ordinal))
            return;

        OnPropertyChanged(nameof(BreedingTabText));
        OnPropertyChanged(nameof(ColorsTabText));
        OnPropertyChanged(nameof(MappingsTabText));
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

public enum LmhEditorHubTab
{
    Breeding,
    Colors,
    Mappings,
    Settings,
    Debug,
}
