using System;
using System.Collections.Generic;
using System.Linq;
using EggsInWild.Core.ShopProgression;
using EggsInWild.Infrastructure.Config;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Mikew.NestCore.Helpers.UI;

namespace EggsInWild.Infrastructure.Integrations.StardewUi.WildAnimals;

public sealed class WildAnimalsEditorViewModel : ObservableObject
{
    private const string FilterAny = "Any";
    private const string FilterMagic = "Magic";
    private const string FilterNoMagic = "No magic";
    private const string FilterLarge = "Large";
    private const string FilterNormal = "Normal";

    private readonly WildAnimalsEditorService _service;
    private readonly Func<ModConfig> _getConfig;
    private readonly Action<ModConfig, WildAnimalDraft, WildAnimalProgressionOverride?> _saveDraft;
    private const string AuthoringModeNone = "";
    private const string AuthoringModeNewAnimal = "newAnimal";
    private const string AuthoringModeNewGroup = "newGroup";

    private WildAnimalsEditorCatalog _catalog = new(new Dictionary<string, EggsInWild.Infrastructure.ModData.Models.WildAnimalJson>(), Array.Empty<WildAnimalsEditorAnimalRecord>(), Array.Empty<string>());
    private string? _selectedKey;
    private string? _pendingSelectedKey;
    private string _statusText = "Select a wild animal to edit.";
    private string _searchText = "";
    private bool _filterOverridden;
    private bool _filterDisabled;
    private bool _filterWarnings;
    private bool _showInvalidAnimals;
    private string _selectedMagicFilter = FilterAny;
    private string _selectedSizeFilter = FilterAny;
    private bool _showSelectionConfirmation;
    private bool _showCloseConfirmation;
    private bool _isBrowsing = true;
    private string _authoringMode = AuthoringModeNone;
    private string _newWildAnimalKey = "";
    private string _authoringModGroupId = "";
    private string _pickerSearchText = "";
    private string _authoringErrorText = "";
    private WildAnimalDraftViewModel? _selectedAnimal;
    private IReadOnlyList<WildAnimalListItemViewModel> _visibleAnimals = Array.Empty<WildAnimalListItemViewModel>();
    private IReadOnlyList<WildAnimalGroupViewModel> _animalGroups = Array.Empty<WildAnimalGroupViewModel>();
    private IReadOnlyList<WildAnimalFarmAnimalPickerItemViewModel> _pickerItems = Array.Empty<WildAnimalFarmAnimalPickerItemViewModel>();
    private int _filteredAnimalCount;
    private int _overrideCount;
    private int _warningCount;

    public WildAnimalsEditorViewModel(
        WildAnimalsEditorService service,
        Func<ModConfig> getConfig,
        Action<ModConfig, WildAnimalDraft, WildAnimalProgressionOverride?> saveDraft)
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
        StartAddWildAnimalCommand = new RelayCommand(StartAddWildAnimalCore);
        StartAddModGroupCommand = new RelayCommand(StartAddModGroupCore, () => SelectedAnimal is not null);
        ConfirmAuthoringCommand = new RelayCommand(ConfirmAuthoringCore, CanConfirmAuthoring);
        CancelAuthoringCommand = new RelayCommand(CancelAuthoringCore, () => IsAuthoring);

        ReloadCatalog();
    }

    public IReadOnlyList<WildAnimalGroupViewModel> AnimalGroups
    {
        get => _animalGroups;
        private set => SetProperty(ref _animalGroups, value);
    }

    public IReadOnlyList<WildAnimalListItemViewModel> VisibleAnimals
    {
        get => _visibleAnimals;
        private set => SetProperty(ref _visibleAnimals, value);
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
            OnPropertyChanged(nameof(HasPendingChanges));
            OnPropertyChanged(nameof(HasHiddenSelection));
            OnPropertyChanged(nameof(CanOpenPreviousAnimal));
            OnPropertyChanged(nameof(CanOpenNextAnimal));
            OnPropertyChanged(nameof(PageTitle));
            StartAddModGroupCommand.NotifyCanExecuteChanged();
            RefreshCommandStates();
        }
    }

    public bool HasSelectedAnimal => SelectedAnimal is not null;
    public bool HasPendingChanges => SelectedAnimal?.IsDirty == true;
    public bool HasHiddenSelection => SelectedAnimal is not null && !VisibleAnimals.Any(item => item.IsSelected);
    public bool IsBrowsing => _isBrowsing;
    public bool IsShowingDetail => !_isBrowsing && SelectedAnimal is not null;
    public bool IsAuthoring => !string.Equals(_authoringMode, AuthoringModeNone, StringComparison.Ordinal);
    public bool IsNotAuthoring => !IsAuthoring;
    public bool IsBrowsingWithoutAuthoring => IsNotAuthoring && IsBrowsing;
    public bool IsAuthoringNewAnimal => string.Equals(_authoringMode, AuthoringModeNewAnimal, StringComparison.Ordinal);
    public bool IsAuthoringNewGroup => string.Equals(_authoringMode, AuthoringModeNewGroup, StringComparison.Ordinal);

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string PageTitle => SelectedAnimal is null
        ? "Eggs In Wild Editor - Wild Animals"
        : $"Eggs In Wild Editor - {SelectedAnimal.Key}";

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

    public IReadOnlyList<string> MagicFilterOptions { get; } = new[] { FilterAny, FilterMagic, FilterNoMagic };

    public string SelectedMagicFilter
    {
        get => _selectedMagicFilter;
        set
        {
            if (!SetProperty(ref _selectedMagicFilter, string.IsNullOrWhiteSpace(value) ? FilterAny : value))
                return;

            ApplyFilters();
        }
    }

    public IReadOnlyList<string> SizeFilterOptions { get; } = new[] { FilterAny, FilterLarge, FilterNormal };

    public string SelectedSizeFilter
    {
        get => _selectedSizeFilter;
        set
        {
            if (!SetProperty(ref _selectedSizeFilter, string.IsNullOrWhiteSpace(value) ? FilterAny : value))
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
    public RelayCommand StartAddWildAnimalCommand { get; }
    public RelayCommand StartAddModGroupCommand { get; }
    public RelayCommand ConfirmAuthoringCommand { get; }
    public RelayCommand CancelAuthoringCommand { get; }

    public IReadOnlyList<WildAnimalFarmAnimalPickerItemViewModel> PickerItems => _pickerItems;
    public string PickerSummary => $"{SelectedPickerCount} selected";
    public int SelectedPickerCount => _pickerItems.Count(item => item.IsSelected);
    public string AuthoringTitle => IsAuthoringNewAnimal ? "Add Wild Animal" : "Add Mod Group";
    public string AuthoringHelpText => IsAuthoringNewAnimal
        ? "Pick farm animals, then choose the new WildAnimalsList entry key."
        : "Pick farm animals for the new ExtraModAnimalKeys group.";
    public string AuthoringErrorText
    {
        get => _authoringErrorText;
        private set
        {
            if (!SetProperty(ref _authoringErrorText, value))
                return;

            OnPropertyChanged(nameof(HasAuthoringError));
        }
    }
    public bool HasAuthoringError => !string.IsNullOrWhiteSpace(AuthoringErrorText);
    public string NewWildAnimalKey
    {
        get => _newWildAnimalKey;
        set
        {
            if (!SetProperty(ref _newWildAnimalKey, value))
                return;

            OnPropertyChanged(nameof(HasNewWildAnimalKey));
            ConfirmAuthoringCommand.NotifyCanExecuteChanged();
        }
    }
    public bool HasNewWildAnimalKey => !string.IsNullOrWhiteSpace(NewWildAnimalKey);
    public string AuthoringModGroupId
    {
        get => _authoringModGroupId;
        set
        {
            if (!SetProperty(ref _authoringModGroupId, value))
                return;

            ConfirmAuthoringCommand.NotifyCanExecuteChanged();
        }
    }
    public string PickerSearchText
    {
        get => _pickerSearchText;
        set
        {
            if (!SetProperty(ref _pickerSearchText, value))
                return;

            ApplyPickerFilter();
        }
    }

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
    public bool CanOpenPreviousAnimal => GetVisibleAnimals().Count > 1 && GetSelectedAnimalIndex() >= 0;
    public bool CanOpenNextAnimal => GetVisibleAnimals().Count > 1 && GetSelectedAnimalIndex() >= 0;
    public void BackToBrowser()
    {
        if (IsAuthoring)
        {
            CancelAuthoringCore();
            return;
        }

        if (SetProperty(ref _isBrowsing, true, nameof(IsBrowsing)))
        {
            OnPropertyChanged(nameof(IsShowingDetail));
            StatusText = SelectedAnimal is null
                ? "Browse wild animals."
                : $"Browsing wild animals. Selected: {SelectedAnimal.Key}.";
        }

        ApplyFilters();
    }
    public void ConfirmSelectionChange() => ConfirmSelectionChangeCommand.Execute(null);
    public void CancelSelectionChange() => CancelSelectionChangeCommand.Execute(null);
    public void ConfirmClose() => ConfirmCloseCommand.Execute(null);
    public void CancelClose() => CancelCloseCommand.Execute(null);
    public void OpenPreviousAnimal() => OpenAdjacentAnimal(-1);
    public void OpenNextAnimal() => OpenAdjacentAnimal(1);
    public void StartAddWildAnimal() => StartAddWildAnimalCommand.Execute(null);
    public void StartAddModGroup() => StartAddModGroupCommand.Execute(null);
    public void ConfirmAuthoring() => ConfirmAuthoringCommand.Execute(null);
    public void CancelAuthoring() => CancelAuthoringCommand.Execute(null);

    private void ReloadCatalogWithGuard()
    {
        if (IsAuthoring)
        {
            StatusText = "Finish or cancel the current authoring action before reloading.";
            return;
        }

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
            SetSelectedAnimal(_selectedKey, announce: $"Reloaded {_selectedKey}.", showDetail: _isBrowsing ? false : IsShowingDetail);
            return;
        }

        _selectedKey = null;
        SelectedAnimal = null;
        if (_catalog.Animals.Count == 0)
        {
            StatusText = "No wild animals were found in WildAnimalsList.";
            return;
        }
        StatusText = "Select a wild animal to edit.";
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
            .GroupBy(_ => "All")
            .Select(group => new WildAnimalGroupViewModel(group.Key, group.Select(CreateListItem).ToList()))
            .ToList();

        VisibleAnimals = filtered
            .Select(CreateListItem)
            .ToList();

        OnPropertyChanged(nameof(HasHiddenSelection));
        OnPropertyChanged(nameof(CanOpenPreviousAnimal));
        OnPropertyChanged(nameof(CanOpenNextAnimal));
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
        if (string.Equals(SelectedMagicFilter, FilterMagic, StringComparison.Ordinal) && !animal.EffectiveIsMagic)
            return false;
        if (string.Equals(SelectedMagicFilter, FilterNoMagic, StringComparison.Ordinal) && animal.EffectiveIsMagic)
            return false;
        if (string.Equals(SelectedSizeFilter, FilterLarge, StringComparison.Ordinal) && !animal.EffectiveLargeEgg)
            return false;
        if (string.Equals(SelectedSizeFilter, FilterNormal, StringComparison.Ordinal) && animal.EffectiveLargeEgg)
            return false;
        if (FilterWarnings && !animal.HasWarnings)
            return false;
        if (!ShowInvalidAnimals && !animal.HasValidEffectiveAnimalKeys)
            return false;

        return true;
    }

    private void RequestSelection(string key)
    {
        if (IsAuthoring)
        {
            StatusText = "Finish or cancel the current authoring action before switching animals.";
            return;
        }

        if (SelectedAnimal?.IsDirty == true && !string.Equals(SelectedAnimal.Key, key, StringComparison.OrdinalIgnoreCase))
        {
            _pendingSelectedKey = key;
            ShowSelectionConfirmation = true;
            StatusText = $"Unsaved changes for {SelectedAnimal.Key}. Confirm before switching to {key}.";
            RefreshCommandStates();
            return;
        }

        SetSelectedAnimal(key, announce: $"Editing {key}.", showDetail: true);
    }

    private void SetSelectedAnimal(string key, string announce, bool showDetail)
    {
        _selectedKey = key;
        _pendingSelectedKey = null;
        _authoringMode = AuthoringModeNone;
        ShowSelectionConfirmation = false;
        ShowCloseConfirmation = false;
        AuthoringErrorText = "";
        _pickerItems = Array.Empty<WildAnimalFarmAnimalPickerItemViewModel>();
        PickerSearchText = "";
        RaiseAuthoringStateChanged();
        SelectedAnimal = CreateSelectedAnimalViewModel(key);
        if (SetProperty(ref _isBrowsing, !showDetail, nameof(IsBrowsing)))
        {
            OnPropertyChanged(nameof(IsShowingDetail));
        }
        else
        {
            OnPropertyChanged(nameof(IsShowingDetail));
        }
        StatusText = announce;
        ApplyFilters();
    }

    private void SaveSelectedAnimal()
    {
        if (SelectedAnimal is null || !SelectedAnimal.TryBuildDraft(out var draft, out _))
            return;

        var config = _getConfig();
        _saveDraft(config, draft, SelectedAnimal.BuildProgressionOverride());
        _selectedKey = draft.Key;
        SelectedAnimal = CreateSelectedAnimalViewModel(draft.Key);
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
        SelectedAnimal = CreateSelectedAnimalViewModel(key);
        ShowSelectionConfirmation = false;
        ShowCloseConfirmation = false;
        StatusText = $"Discarded unsaved changes for {key}.";
        ApplyFilters();
    }

    private void ConfirmSelectionChangeCore()
    {
        if (string.IsNullOrWhiteSpace(_pendingSelectedKey))
            return;

        SetSelectedAnimal(_pendingSelectedKey, announce: $"Editing {_pendingSelectedKey}.", showDetail: true);
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
                OnPropertyChanged(nameof(HasPendingChanges));
                StartAddModGroupCommand.NotifyCanExecuteChanged();
                RefreshCommandStates();
                break;
        }
    }

    private IReadOnlyList<WildAnimalListItemViewModel> GetVisibleAnimals()
        => VisibleAnimals;

    private int GetSelectedAnimalIndex()
    {
        if (SelectedAnimal is null)
            return -1;

        var visible = GetVisibleAnimals();
        for (var i = 0; i < visible.Count; i++)
        {
            if (string.Equals(visible[i].Key, SelectedAnimal.Key, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private void OpenAdjacentAnimal(int delta)
    {
        var visible = GetVisibleAnimals();
        var currentIndex = GetSelectedAnimalIndex();
        if (currentIndex < 0 || visible.Count == 0)
            return;

        var nextIndex = (currentIndex + delta + visible.Count) % visible.Count;

        RequestSelection(visible[nextIndex].Key);
    }

    private void RefreshCommandStates()
    {
        ((RelayCommand)SaveCommand).NotifyCanExecuteChanged();
        ((RelayCommand)DiscardCommand).NotifyCanExecuteChanged();
        ConfirmSelectionChangeCommand.NotifyCanExecuteChanged();
        CancelSelectionChangeCommand.NotifyCanExecuteChanged();
        ConfirmCloseCommand.NotifyCanExecuteChanged();
        CancelCloseCommand.NotifyCanExecuteChanged();
        ConfirmAuthoringCommand.NotifyCanExecuteChanged();
        CancelAuthoringCommand.NotifyCanExecuteChanged();
    }

    private void StartAddWildAnimalCore()
    {
        if (SelectedAnimal?.IsDirty == true)
        {
            StatusText = $"Save or discard changes for {SelectedAnimal.Key} before creating a new wild animal.";
            return;
        }

        _authoringMode = AuthoringModeNewAnimal;
        NewWildAnimalKey = "";
        AuthoringModGroupId = "";
        AuthoringErrorText = "";
        LoadPickerItems(Array.Empty<string>());
        SetBrowsingState(false);
        StatusText = "Pick farm animals for the new wild animal.";
        RaiseAuthoringStateChanged();
    }

    private void StartAddModGroupCore()
    {
        if (SelectedAnimal is null)
            return;

        if (SelectedAnimal.IsDirty)
        {
            StatusText = $"Save or discard changes for {SelectedAnimal.Key} before adding a new mod group.";
            return;
        }

        _authoringMode = AuthoringModeNewGroup;
        NewWildAnimalKey = "";
        AuthoringModGroupId = "";
        AuthoringErrorText = "";
        var referencedKeys = SelectedAnimal.ModGroups.SelectMany(group => WildAnimalsEditorHelpers.ParseCsvKeyList(group.KeysText)).ToList();
        LoadPickerItems(referencedKeys);
        SetBrowsingState(false);
        StatusText = $"Pick farm animals for a new mod group on {SelectedAnimal.Key}.";
        RaiseAuthoringStateChanged();
    }

    private void LoadPickerItems(IEnumerable<string> referencedKeys)
    {
        _pickerItems = _service.LoadFarmAnimalPickerItems(referencedKeys)
            .Select(item => new WildAnimalFarmAnimalPickerItemViewModel(
                item.Key,
                item.DisplayName,
                item.TooltipText,
                item.SuggestedModId,
                _service.TryLoadPreviewSprite(item.Preview),
                item.Preview.StatusText,
                TogglePickerAnimalSelection))
            .ToList();
        ApplyPickerFilter();
        OnPropertyChanged(nameof(PickerItems));
        OnPropertyChanged(nameof(PickerSummary));
        OnPropertyChanged(nameof(SelectedPickerCount));
    }

    private void ApplyPickerFilter()
    {
        var search = PickerSearchText.Trim();
        foreach (var item in _pickerItems)
        {
            item.IsVisible = search.Length == 0
                || item.Key.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                || item.DisplayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        OnPropertyChanged(nameof(PickerItems));
    }

    private void TogglePickerAnimalSelection(WildAnimalFarmAnimalPickerItemViewModel item)
    {
        item.IsSelected = !item.IsSelected;
        if (IsAuthoringNewGroup && string.IsNullOrWhiteSpace(AuthoringModGroupId) && item.IsSelected)
            AuthoringModGroupId = item.SuggestedModId;

        AuthoringErrorText = "";
        OnPropertyChanged(nameof(PickerSummary));
        OnPropertyChanged(nameof(SelectedPickerCount));
        ConfirmAuthoringCommand.NotifyCanExecuteChanged();
    }

    private bool CanConfirmAuthoring()
    {
        if (!IsAuthoring)
            return false;

        if (_pickerItems.Count(item => item.IsSelected) == 0)
            return false;

        return IsAuthoringNewAnimal
            ? !string.IsNullOrWhiteSpace(NewWildAnimalKey)
            : !string.IsNullOrWhiteSpace(AuthoringModGroupId);
    }

    private void ConfirmAuthoringCore()
    {
        try
        {
            var selectedKeys = _pickerItems.Where(item => item.IsSelected).Select(item => item.Key).ToList();
            if (IsAuthoringNewAnimal)
            {
                _service.CreateWildAnimalDefinition(NewWildAnimalKey.Trim(), selectedKeys, _catalog.LocationTypeKeys);
                ReloadCatalog();
                SetSelectedAnimal(NewWildAnimalKey.Trim(), announce: $"Created {NewWildAnimalKey.Trim()}.", showDetail: true);
                return;
            }

            if (SelectedAnimal is null)
                return;

            _service.AddOrUpdateModGroupDefinition(SelectedAnimal.Key, AuthoringModGroupId.Trim(), selectedKeys);
            var currentKey = SelectedAnimal.Key;
            ReloadCatalog();
            SetSelectedAnimal(currentKey, announce: $"Added {AuthoringModGroupId.Trim()} to {currentKey}.", showDetail: true);
        }
        catch (Exception ex)
        {
            AuthoringErrorText = ex.Message;
            StatusText = ex.Message;
        }
    }

    private void CancelAuthoringCore()
    {
        _authoringMode = AuthoringModeNone;
        NewWildAnimalKey = "";
        AuthoringModGroupId = "";
        PickerSearchText = "";
        AuthoringErrorText = "";
        _pickerItems = Array.Empty<WildAnimalFarmAnimalPickerItemViewModel>();
        RaiseAuthoringStateChanged();
        SetBrowsingState(SelectedAnimal is null);
        StatusText = SelectedAnimal is null ? "Browsing wild animals." : $"Editing {SelectedAnimal.Key}.";
    }

    private void SetBrowsingState(bool isBrowsing)
    {
        if (SetProperty(ref _isBrowsing, isBrowsing, nameof(IsBrowsing)))
        {
            OnPropertyChanged(nameof(IsShowingDetail));
            OnPropertyChanged(nameof(IsBrowsingWithoutAuthoring));
        }
        else
        {
            OnPropertyChanged(nameof(IsShowingDetail));
            OnPropertyChanged(nameof(IsBrowsingWithoutAuthoring));
        }
    }

    private void RaiseAuthoringStateChanged()
    {
        OnPropertyChanged(nameof(IsAuthoring));
        OnPropertyChanged(nameof(IsNotAuthoring));
        OnPropertyChanged(nameof(IsBrowsingWithoutAuthoring));
        OnPropertyChanged(nameof(IsAuthoringNewAnimal));
        OnPropertyChanged(nameof(IsAuthoringNewGroup));
        OnPropertyChanged(nameof(AuthoringTitle));
        OnPropertyChanged(nameof(AuthoringHelpText));
        OnPropertyChanged(nameof(HasAuthoringError));
    }

    private WildAnimalDraftViewModel CreateSelectedAnimalViewModel(string key)
    {
        var record = _catalog.Animals.First(animal => string.Equals(animal.Key, key, StringComparison.OrdinalIgnoreCase));
        var config = _getConfig();
        config.AnimalProgressionMode ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        var progressionAnimalKey = record.Preview.FarmAnimalKey
            ?? record.ModGroups.SelectMany(group => group.EffectiveKeys).FirstOrDefault();
        var progressionDefaultEnabled = !string.IsNullOrWhiteSpace(progressionAnimalKey)
            && AnimalShopProgressionPolicy.DefaultEnabled(progressionAnimalKey);
        var progressionConfiguredEnabled = !string.IsNullOrWhiteSpace(progressionAnimalKey)
            && config.AnimalProgressionMode.TryGetValue(progressionAnimalKey, out var configured)
                ? configured
                : (bool?)null;

        return new WildAnimalDraftViewModel(
            _service,
            _service.CreateDraft(key),
            progressionAnimalKey,
            progressionDefaultEnabled,
            progressionConfiguredEnabled);
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
    public bool MissingPreviewSprite => !HasPreviewSprite;
    public RelayCommand Open { get; }
    public void OpenAnimal() => Open.Execute(null);

    public string ButtonText => "";
    public string MetaText => string.IsNullOrWhiteSpace(BadgesSummary) ? PreviewStatus : $"{PreviewStatus} | {BadgesSummary}";
    public string DisplayTooltip => $"{DisplayName}\n{MetaText}";
    public string SelectionText => IsSelected ? "[Selected]" : "";
}

public sealed record WildAnimalBadgeViewModel(string Text);

public sealed class WildAnimalFarmAnimalPickerItemViewModel : ObservableObject
{
    private readonly Action<WildAnimalFarmAnimalPickerItemViewModel> _toggleSelection;
    private bool _isSelected;
    private bool _isVisible = true;

    public WildAnimalFarmAnimalPickerItemViewModel(
        string key,
        string displayName,
        string tooltipText,
        string suggestedModId,
        Tuple<Texture2D, Rectangle>? previewSprite,
        string previewStatus,
        Action<WildAnimalFarmAnimalPickerItemViewModel> toggleSelection)
    {
        Key = key;
        DisplayName = displayName;
        TooltipText = tooltipText;
        SuggestedModId = suggestedModId;
        PreviewSprite = previewSprite;
        PreviewStatus = previewStatus;
        _toggleSelection = toggleSelection;
        ToggleSelectionCommand = new RelayCommand(() => _toggleSelection(this));
    }

    public string Key { get; }
    public string DisplayName { get; }
    public string TooltipText { get; }
    public string SuggestedModId { get; }
    public Tuple<Texture2D, Rectangle>? PreviewSprite { get; }
    public string PreviewStatus { get; }
    public bool HasPreviewSprite => PreviewSprite is not null;
    public bool MissingPreviewSprite => !HasPreviewSprite;
    public RelayCommand ToggleSelectionCommand { get; }
    public void ToggleSelection() => ToggleSelectionCommand.Execute(null);

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (!SetProperty(ref _isSelected, value))
                return;

            OnPropertyChanged(nameof(SelectionText));
        }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public string SelectionText => IsSelected ? "[Selected]" : "";
}
