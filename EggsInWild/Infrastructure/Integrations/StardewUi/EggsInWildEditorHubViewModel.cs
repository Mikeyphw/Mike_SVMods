using System;
using System.ComponentModel;
using EggsInWild.Infrastructure.Integrations.StardewUi.WildAnimals;
using Mikew.NestCore.Helpers.UI;

namespace EggsInWild.Infrastructure.Integrations.StardewUi;

public sealed class EggsInWildEditorHubViewModel : ObservableObject
{
    private const string WildAnimalsTabKey = "wildAnimals";

    private string _selectedTab;

    public EggsInWildEditorHubViewModel(
        WildAnimalsEditorViewModel wildAnimals,
        string initialTab)
    {
        WildAnimals = wildAnimals ?? throw new ArgumentNullException(nameof(wildAnimals));
        _selectedTab = WildAnimalsTabKey;
        WildAnimals.PropertyChanged += OnChildPropertyChanged;
    }

    public WildAnimalsEditorViewModel WildAnimals { get; }

    public string Title => WildAnimals.PageTitle;

    public bool IsWildAnimalsTab => string.Equals(_selectedTab, WildAnimalsTabKey, StringComparison.Ordinal);

    public string WildAnimalsTabText => FormatTabText("Wild Animals", IsWildAnimalsTab, WildAnimals.HasPendingChanges);

    public void ShowWildAnimalsTab() => SetTab(WildAnimalsTabKey);

    public bool CanCloseMenu()
        => !IsWildAnimalsTab || WildAnimals.CanCloseMenu();

    private void SetTab(string tab)
    {
        if (!SetProperty(ref _selectedTab, tab, nameof(SelectedTab)))
            return;

        OnPropertyChanged(nameof(SelectedTab));
        OnPropertyChanged(nameof(IsWildAnimalsTab));
        OnPropertyChanged(nameof(WildAnimalsTabText));
    }

    private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(WildAnimalsEditorViewModel.HasPendingChanges):
                OnPropertyChanged(nameof(WildAnimalsTabText));
                break;
            case nameof(WildAnimalsEditorViewModel.PageTitle):
                OnPropertyChanged(nameof(Title));
                break;
        }
    }

    private static string FormatTabText(string label, bool isSelected, bool hasChanges)
    {
        var text = hasChanges ? $"{label} *" : label;
        return isSelected ? $"[{text}]" : text;
    }

    private string SelectedTab => _selectedTab;
}
