using System;
using System.Collections.Generic;
using System.Linq;
using EggsInWild.Infrastructure.Config;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Mikew.NestCore.Helpers.UI;

namespace EggsInWild.Infrastructure.Integrations.StardewUi.WildAnimals;

public sealed class WildAnimalsEditorViewModel : ObservableObject
{
    private readonly WildAnimalsEditorService _service;
    private readonly Func<ModConfig> _getConfig;
    private readonly Action<ModConfig, WildAnimalDraft> _saveDraft;

    private WildAnimalsEditorCatalog _catalog = new(new Dictionary<string, EggsInWild.Infrastructure.ModData.Models.WildAnimalJson>(), Array.Empty<WildAnimalsEditorAnimalRecord>(), Array.Empty<string>());
    private string? _selectedKey;
    private string? _pendingSelectedKey;
    private string _statusText = "Select a wild animal to edit.";
    private string _searchText = "";
    private bool _filterOverridden;
    private bool _filterDisabled;
    private bool _filterMagic;
    private bool _filterWarnings;
    private bool _showInvalidAnimals;
    private bool _showSelectionConfirmation;
    private bool _showCloseConfirmation;
    private WildAnimalDraftViewModel? _selectedAnimal;
    private IReadOnlyList<WildAnimalGroupViewModel> _animalGroups = Array.Empty<WildAnimalGroupViewModel>();
    private int _filteredAnimalCount;
    private int _overrideCount;
    private int _warningCount;

    public WildAnimalsEditorViewModel(
        WildAnimalsEditorService service,
        Func<ModConfig> getConfig,
        Action<ModConfig, WildAnimalDraft> saveDraft)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _getConfig = getConfig ?? throw new ArgumentNullException(nameof(getConfig));
        _saveDraft = saveDraft ?? throw new ArgumentNullException(nameof(saveDraft));

        SaveCommand = new RelayCommand(SaveSelectedAnimal, CanSaveSelectedAnimal);
        DiscardCommand = new RelayCommand(DiscardSelectedAnimal, () => SelectedAnimal?.IsDirty == true);
        ReloadCommand = new RelayCommand(ReloadCatalogWithGuard);
        ConfirmSelectionChangeCommand = new RelayCommand(ConfirmSelectionChangeCore, () => ShowSelectionConfirmation && !string.IsNullOrWhiteSpace(_pendingSelectedKey));
        CancelSelectionChangeCommand = new RelayCommand(CancelSelectionChangeCore, () => ShowSelectionConfirmation);
        ConfirmCloseCommand = new RelayCommand(ConfirmCloseCore, () => ShowCloseConfirmation);
        CancelCloseCommand = new RelayCommand(CancelCloseCore, () => ShowCloseConfirmation);

        ReloadCatalog();
    }

    public IReadOnlyList<WildAnimalGroupViewModel> AnimalGroups
    {
        get => _animalGroups;
        private set => SetProperty(ref _animalGroups, value);
    }

    public WildAnimalDraftViewModel? SelectedAnimal
    {
        get => _selectedAnimal;
        private set
        {
            if (_selectedAnimal is not null)
                _selectedAnimal.PropertyChanged -= OnSelectedAnimalPropertyChanged;

            if (!SetProperty(ref _selectedAnimal, value))
                return;

            if (_selectedAnimal is not null)
                _selectedAnimal.PropertyChanged += OnSelectedAnimalPropertyChanged;

            OnPropertyChanged(nameof(HasSelectedAnimal));
            OnPropertyChanged(nameof(HasHiddenSelection));
            RefreshCommandStates();
        }
    }

    public bool HasSelectedAnimal => SelectedAnimal is not null;
    public bool HasHiddenSelection => SelectedAnimal is not null && !AnimalGroups.SelectMany(group => group.Animals).Any(item => item.IsSelected);

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value))
                return;

            ApplyFilters();
        }
    }

    public bool FilterOverridden
    {
        get => _filterOverridden;
        set
        {
            if (!SetProperty(ref _filterOverridden, value))
                return;

            ApplyFilters();
        }
    }

    public bool FilterDisabled
    {
        get => _filterDisabled;
        set
        {
            if (!SetProperty(ref _filterDisabled, value))
                return;

            ApplyFilters();
        }
    }

    public bool FilterMagic
    {
        get => _filterMagic;
        set
        {
            if (!SetProperty(ref _filterMagic, value))
                return;

            ApplyFilters();
        }
    }

    public bool FilterWarnings
    {
        get => _filterWarnings;
        set
        {
            if (!SetProperty(ref _filterWarnings, value))
                return;

            ApplyFilters();
        }
    }

    public bool ShowInvalidAnimals
    {
        get => _showInvalidAnimals;
        set
        {
            if (!SetProperty(ref _showInvalidAnimals, value))
                return;

            OnPropertyChanged(nameof(VisibilitySummary));
            ApplyFilters();
        }
    }

    public int FilteredAnimalCount
    {
        get => _filteredAnimalCount;
        private set => SetProperty(ref _filteredAnimalCount, value);
    }

    public int TotalAnimalCount => _catalog.Animals.Count;

    public int OverrideCount
    {
        get => _overrideCount;
        private set => SetProperty(ref _overrideCount, value);
    }

    public int WarningCount
    {
        get => _warningCount;
        private set => SetProperty(ref _warningCount, value);
    }

    public bool ShowSelectionConfirmation
    {
        get => _showSelectionConfirmation;
        private set
        {
            if (!SetProperty(ref _showSelectionConfirmation, value))
                return;

            RefreshCommandStates();
        }
    }

    public bool ShowCloseConfirmation
    {
        get => _showCloseConfirmation;
        private set
        {
            if (!SetProperty(ref _showCloseConfirmation, value))
                return;

            RefreshCommandStates();
        }
    }

    public string HeaderSummary => $"Showing {FilteredAnimalCount} of {TotalAnimalCount} animals.";
    public string StatsSummary => $"{OverrideCount} overridden | {WarningCount} warning states";
    public bool ShowEmptyState => FilteredAnimalCount == 0;
    public string VisibilitySummary => ShowInvalidAnimals ? "Invalid groups visible." : "Invalid-only groups hidden.";
    public string DirtyStatusText
    {
        get
        {
            if (SelectedAnimal is null)
                return "No selection";
            if (SelectedAnimal.HasValidationErrors)
                return "Fix validation errors before saving.";
            if (SelectedAnimal.IsDirty)
                return "Unsaved changes";
            return "All changes saved";
        }
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand DiscardCommand { get; }
    public RelayCommand ReloadCommand { get; }
    public RelayCommand ConfirmSelectionChangeCommand { get; }
    public RelayCommand CancelSelectionChangeCommand { get; }
    public RelayCommand ConfirmCloseCommand { get; }
    public RelayCommand CancelCloseCommand { get; }

    public bool CanCloseMenu()
    {
        if (SelectedAnimal?.IsDirty != true)
            return true;

        if (!ShowCloseConfirmation)
        {
            ShowCloseConfirmation = true;
            StatusText = $"Unsaved changes for {SelectedAnimal.Key}. Close again or use the confirmation banner.";
            return false;
        }

        return true;
    }

    public void Save() => SaveCommand.Execute(null);
    public void Discard() => DiscardCommand.Execute(null);
    public void Reload() => ReloadCommand.Execute(null);
    public void ConfirmSelectionChange() => ConfirmSelectionChangeCommand.Execute(null);
    public void CancelSelectionChange() => CancelSelectionChangeCommand.Execute(null);
    public void ConfirmClose() => ConfirmCloseCommand.Execute(null);
    public void CancelClose() => CancelCloseCommand.Execute(null);

    private void ReloadCatalogWithGuard()
    {
        if (SelectedAnimal?.IsDirty == true)
        {
            ShowCloseConfirmation = true;
            StatusText = $"Unsaved changes for {SelectedAnimal.Key}. Discard or save before reloading.";
            return;
        }

        ReloadCatalog();
    }

    private void ReloadCatalog()
    {
        _catalog = _service.LoadCatalog();
        OverrideCount = _catalog.Animals.Count(animal => animal.HasOverrides);
        WarningCount = _catalog.Animals.Count(animal => animal.HasWarnings);
        OnPropertyChanged(nameof(TotalAnimalCount));
        ApplyFilters();

        if (_selectedKey is not null && _catalog.Animals.Any(animal => string.Equals(animal.Key, _selectedKey, StringComparison.OrdinalIgnoreCase)))
        {
            SetSelectedAnimal(_selectedKey, announce: $"Reloaded {_selectedKey}.");
            return;
        }

        var first = _catalog.Animals.FirstOrDefault();
        if (first is null)
        {
            SelectedAnimal = null;
            StatusText = "No wild animals were found in WildAnimalsList.";
            return;
        }

        SetSelectedAnimal(first.Key, announce: $"Editing {first.Key}.");
    }

    private void ApplyFilters()
    {
        var filtered = _catalog.Animals
            .Where(MatchesFilters)
            .ToList();

        FilteredAnimalCount = filtered.Count;
        OnPropertyChanged(nameof(HeaderSummary));
        OnPropertyChanged(nameof(StatsSummary));
        OnPropertyChanged(nameof(ShowEmptyState));

        AnimalGroups = filtered
            .GroupBy(animal => GetGroupKey(animal.Key))
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new WildAnimalGroupViewModel(
                group.Key,
                group.Select(CreateListItem)
                    .ToList()))
            .ToList();

        OnPropertyChanged(nameof(HasHiddenSelection));
        if (filtered.Count == 0)
            StatusText = "No animals match the current search and filters.";
    }

    private WildAnimalListItemViewModel CreateListItem(WildAnimalsEditorAnimalRecord animal)
    {
        var badges = new List<WildAnimalBadgeViewModel>();
        if (!animal.EffectiveEnabled)
            badges.Add(new WildAnimalBadgeViewModel("Disabled"));
        if (animal.EffectiveIsMagic)
            badges.Add(new WildAnimalBadgeViewModel("Magic"));
        if (animal.HasOverrides)
            badges.Add(new WildAnimalBadgeViewModel("Override"));
        if (animal.HasWarnings)
            badges.Add(new WildAnimalBadgeViewModel("Warning"));

        return new WildAnimalListItemViewModel(
            animal.Key,
            animal.Key,
            animal.Preview.StatusText,
            badges,
            string.Join(" | ", badges.Select(badge => badge.Text)),
            string.Equals(_selectedKey, animal.Key, StringComparison.OrdinalIgnoreCase),
            _service.TryLoadPreviewSprite(animal.Preview),
            new RelayCommand(() => RequestSelection(animal.Key)));
    }

    private bool MatchesFilters(WildAnimalsEditorAnimalRecord animal)
    {
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            if (animal.Key.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                return false;
        }

        if (FilterOverridden && !animal.HasOverrides)
            return false;
        if (FilterDisabled && animal.EffectiveEnabled)
            return false;
        if (FilterMagic && !animal.EffectiveIsMagic)
            return false;
        if (FilterWarnings && !animal.HasWarnings)
            return false;
        if (!ShowInvalidAnimals && !animal.HasValidEffectiveAnimalKeys)
            return false;

        return true;
    }

    private void RequestSelection(string key)
    {
        if (SelectedAnimal?.IsDirty == true && !string.Equals(SelectedAnimal.Key, key, StringComparison.OrdinalIgnoreCase))
        {
            _pendingSelectedKey = key;
            ShowSelectionConfirmation = true;
            StatusText = $"Unsaved changes for {SelectedAnimal.Key}. Confirm before switching to {key}.";
            RefreshCommandStates();
            return;
        }

        SetSelectedAnimal(key, announce: $"Editing {key}.");
    }

    private void SetSelectedAnimal(string key, string announce)
    {
        _selectedKey = key;
        _pendingSelectedKey = null;
        ShowSelectionConfirmation = false;
        ShowCloseConfirmation = false;
        SelectedAnimal = new WildAnimalDraftViewModel(_service, _service.CreateDraft(key));
        StatusText = announce;
        ApplyFilters();
    }

    private void SaveSelectedAnimal()
    {
        if (SelectedAnimal is null || !SelectedAnimal.TryBuildDraft(out var draft, out _))
            return;

        var config = _getConfig();
        _saveDraft(config, draft);
        _selectedKey = draft.Key;
        SelectedAnimal = new WildAnimalDraftViewModel(_service, _service.CreateDraft(draft.Key));
        StatusText = $"Saved {draft.Key}.";
        ReloadCatalog();
    }

    private bool CanSaveSelectedAnimal()
    {
        return SelectedAnimal is not null
            && SelectedAnimal.IsDirty
            && !SelectedAnimal.HasValidationErrors;
    }

    private void DiscardSelectedAnimal()
    {
        if (SelectedAnimal is null)
            return;

        var key = SelectedAnimal.Key;
        SelectedAnimal = new WildAnimalDraftViewModel(_service, _service.DiscardDraft(key));
        ShowSelectionConfirmation = false;
        ShowCloseConfirmation = false;
        StatusText = $"Discarded unsaved changes for {key}.";
        ApplyFilters();
    }

    private void ConfirmSelectionChangeCore()
    {
        if (string.IsNullOrWhiteSpace(_pendingSelectedKey))
            return;

        SetSelectedAnimal(_pendingSelectedKey, announce: $"Editing {_pendingSelectedKey}.");
    }

    private void CancelSelectionChangeCore()
    {
        _pendingSelectedKey = null;
        ShowSelectionConfirmation = false;
        StatusText = SelectedAnimal is null ? "Selection unchanged." : $"Stayed on {SelectedAnimal.Key}.";
    }

    private void ConfirmCloseCore()
    {
        ShowCloseConfirmation = false;
        StatusText = SelectedAnimal is null ? "Close confirmation cleared." : $"Close confirmation armed for {SelectedAnimal.Key}.";
    }

    private void CancelCloseCore()
    {
        ShowCloseConfirmation = false;
        StatusText = SelectedAnimal is null ? "Close canceled." : $"Close canceled for {SelectedAnimal.Key}.";
    }

    private void OnSelectedAnimalPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(WildAnimalDraftViewModel.IsDirty):
            case nameof(WildAnimalDraftViewModel.HasValidationErrors):
            case nameof(WildAnimalDraftViewModel.HeaderStatus):
                OnPropertyChanged(nameof(DirtyStatusText));
                RefreshCommandStates();
                break;
        }
    }

    private void RefreshCommandStates()
    {
        ((RelayCommand)SaveCommand).NotifyCanExecuteChanged();
        ((RelayCommand)DiscardCommand).NotifyCanExecuteChanged();
        ConfirmSelectionChangeCommand.NotifyCanExecuteChanged();
        CancelSelectionChangeCommand.NotifyCanExecuteChanged();
        ConfirmCloseCommand.NotifyCanExecuteChanged();
        CancelCloseCommand.NotifyCanExecuteChanged();
    }

    private static string GetGroupKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "Other";

        var first = char.ToUpperInvariant(key.Trim()[0]);
        if (first is >= 'A' and <= 'F') return "A-F";
        if (first is >= 'G' and <= 'L') return "G-L";
        if (first is >= 'M' and <= 'R') return "M-R";
        if (first is >= 'S' and <= 'Z') return "S-Z";
        return "Other";
    }
}

public sealed record WildAnimalGroupViewModel(string Title, IReadOnlyList<WildAnimalListItemViewModel> Animals);

public sealed class WildAnimalListItemViewModel : ObservableObject
{
    public WildAnimalListItemViewModel(
        string key,
        string displayName,
        string previewStatus,
        IReadOnlyList<WildAnimalBadgeViewModel> badges,
        string badgesSummary,
        bool isSelected,
        Tuple<Texture2D, Rectangle>? previewSprite,
        RelayCommand open)
    {
        Key = key;
        DisplayName = displayName;
        PreviewStatus = previewStatus;
        Badges = badges;
        BadgesSummary = badgesSummary;
        IsSelected = isSelected;
        PreviewSprite = previewSprite;
        Open = open;
    }

    public string Key { get; }
    public string DisplayName { get; }
    public string PreviewStatus { get; }
    public IReadOnlyList<WildAnimalBadgeViewModel> Badges { get; }
    public string BadgesSummary { get; }
    public bool IsSelected { get; }
    public Tuple<Texture2D, Rectangle>? PreviewSprite { get; }
    public bool HasPreviewSprite => PreviewSprite is not null;
    public RelayCommand Open { get; }
    public void OpenAnimal() => Open.Execute(null);

    public string ButtonText => IsSelected ? $"> {DisplayName}" : DisplayName;
    public string MetaText => string.IsNullOrWhiteSpace(BadgesSummary) ? PreviewStatus : $"{PreviewStatus} | {BadgesSummary}";
}

public sealed record WildAnimalBadgeViewModel(string Text);
