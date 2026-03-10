using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using RoosterControl.Application.Compat;
using RoosterControl.Enums;
using StardewValley.GameData.FarmAnimals;

namespace RoosterControl.Integrations.StardewUi.Compat;

/// <summary>
/// View model for the StardewUI compat editor.
/// </summary>
internal sealed class RoosterCompatEditorViewModel : INotifyPropertyChanged
{
    private readonly RoosterCompatEditorService _service;
    private readonly CompatSelectionState _state;
    private readonly Func<IDictionary<string, FarmAnimalData>> _getAnimalData;
    private readonly Action _saveChanges;
    private RoosterCompatEditorDraft _draft;
    private RoosterCompatEditorSnapshot? _snapshot;

    public event PropertyChangedEventHandler? PropertyChanged;

    public RoosterCompatEditorViewModel(
        RoosterCompatEditorService service,
        CompatSelectionState state,
        Func<IDictionary<string, FarmAnimalData>> getAnimalData,
        Action saveChanges)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _getAnimalData = getAnimalData ?? throw new ArgumentNullException(nameof(getAnimalData));
        _saveChanges = saveChanges ?? throw new ArgumentNullException(nameof(saveChanges));
        _draft = new RoosterCompatEditorDraft();

        RefreshSnapshot();
    }

    public string Title => "Compat Editor";

    public string SummaryText => _snapshot is not null
        ? _service.BuildSummaryText(_state, _getAnimalData())
        : "Loading...";

    public string ValidationText { get; private set; } = string.Empty;

    public bool HasValidation => !string.IsNullOrWhiteSpace(ValidationText);

    public IReadOnlyList<RoosterCompatEggGroupViewModel> EggGroups { get; private set; } = Array.Empty<RoosterCompatEggGroupViewModel>();

    public RoosterCompatEggGroupViewModel? SelectedEggGroup { get; private set; }
    public bool HasSelectedEggGroup => SelectedEggGroup is not null;
    public string SelectedEggGroupName => SelectedEggGroup?.Category ?? "(none)";

    public IReadOnlyList<RoosterCompatProviderViewModel> ProviderOptions { get; private set; } = Array.Empty<RoosterCompatProviderViewModel>();

    public RoosterCompatProviderViewModel? SelectedProvider { get; private set; }
    public string SelectedProviderName => SelectedProvider?.DisplayName ?? "All providers";

    public bool CanSave => HasChanges;

    public bool CanDiscard => HasChanges;

    public bool HasChanges { get; private set; }

    public void RefreshSnapshot()
    {
        var animalData = _getAnimalData();
        _snapshot = _service.LoadSnapshot(_state, animalData);

        // Build egg groups
        var eggGroupVMs = new List<RoosterCompatEggGroupViewModel>();
        foreach (var group in _snapshot.EggGroups)
        {
            var vm = new RoosterCompatEggGroupViewModel(group, SelectEggGroup);
            eggGroupVMs.Add(vm);
        }
        EggGroups = eggGroupVMs;

        // Build provider options
        var providerVMs = new List<RoosterCompatProviderViewModel>();
        foreach (var provider in _snapshot.ProviderRecords)
        {
            var vm = new RoosterCompatProviderViewModel(provider, SelectProvider);
            providerVMs.Add(vm);
        }
        ProviderOptions = providerVMs;

        // Determine selected state
        UpdateSelectedState();
        ValidationText = BuildValidationText(animalData);

        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(ValidationText));
        OnPropertyChanged(nameof(HasValidation));
        OnPropertyChanged(nameof(EggGroups));
        OnPropertyChanged(nameof(ProviderOptions));
    }

    private void UpdateSelectedState()
    {
        // Find first egg group with selections
        SelectedEggGroup = EggGroups.FirstOrDefault(g => g.Kinds.Any(k => k.IsSelected));

        // Find selected provider
        var enabledProviders = _state.EnabledRoosterSources ?? new List<string>();
        if (enabledProviders.Count == 1)
        {
            SelectedProvider = ProviderOptions.FirstOrDefault(p => 
                p.ProviderId.Equals(enabledProviders[0], StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            SelectedProvider = null;
        }

        OnPropertyChanged(nameof(SelectedEggGroup));
        OnPropertyChanged(nameof(HasSelectedEggGroup));
        OnPropertyChanged(nameof(SelectedProvider));
        OnPropertyChanged(nameof(SelectedEggGroupName));
        OnPropertyChanged(nameof(SelectedProviderName));
    }

    public void SelectEggGroup(RoosterCompatEggGroupViewModel group)
    {
        SelectedEggGroup = group;
        OnPropertyChanged(nameof(SelectedEggGroup));
        OnPropertyChanged(nameof(HasSelectedEggGroup));
        OnPropertyChanged(nameof(SelectedEggGroupName));
    }

    public void SelectProvider(RoosterCompatProviderViewModel provider)
    {
        SelectedProvider = provider;
        _draft.UseAllProviders = false;
        _draft.SelectedProviderId = provider.ProviderId;
        HasChanges = true;
        OnPropertyChanged(nameof(SelectedProvider));
        OnPropertyChanged(nameof(SelectedProviderName));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
    }

    public void SetEggSource(EggKind kind, string? sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return;

        _draft.EggSelections ??= new Dictionary<EggKind, string>();
        _draft.EggSelections[kind] = sourceId;
        HasChanges = true;
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
    }

    public void SetUseAllProviders(bool useAll)
    {
        _draft.UseAllProviders = useAll;
        if (useAll)
        {
            _draft.SelectedProviderId = null;
            SelectedProvider = null;
            OnPropertyChanged(nameof(SelectedProvider));
            OnPropertyChanged(nameof(SelectedProviderName));
        }
        HasChanges = true;
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
    }

    public void UseAllProviders()
    {
        SetUseAllProviders(true);
    }

    public void Save()
    {
        if (!HasChanges)
            return;

        _service.ApplyDraft(_state, _draft);
        _saveChanges();
        _draft = new RoosterCompatEditorDraft();
        HasChanges = false;

        RefreshSnapshot();
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
    }

    public void Discard()
    {
        _draft = new RoosterCompatEditorDraft();
        HasChanges = false;

        RefreshSnapshot();
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
    }

    public void Reload()
    {
        Discard();
    }

    public void ResetSelection()
    {
        _draft.EggSelections = new Dictionary<EggKind, string>();
        _draft.UseAllProviders = true;
        _draft.SelectedProviderId = null;

        foreach (var group in EggGroups)
        {
            foreach (var kind in group.Kinds)
            {
                kind.ResetToDefault();
            }
        }

        SelectedProvider = null;
        HasChanges = true;
        OnPropertyChanged(nameof(SelectedProvider));
        OnPropertyChanged(nameof(SelectedProviderName));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
    }

    private string BuildValidationText(IDictionary<string, FarmAnimalData> animalData)
    {
        if (_snapshot is null)
            return string.Empty;

        var lines = new List<string>();
        foreach (var group in _snapshot.EggGroups)
        {
            foreach (var kind in group.Kinds)
            {
                if (!string.IsNullOrWhiteSpace(kind.ValidationNote))
                    lines.Add(kind.ValidationNote);
            }
        }

        lines.Add(_service.BuildRoosterProvidersValidationNote(_state, animalData));
        return string.Join("\n", lines);
    }

    private void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

internal sealed class RoosterCompatEggGroupViewModel
{
    private readonly Action<RoosterCompatEggGroupViewModel> _onSelect;

    public RoosterCompatEggGroupViewModel(RoosterCompatEggGroupRecord record, Action<RoosterCompatEggGroupViewModel> onSelect)
    {
        _onSelect = onSelect;
        Category = record.Category;
        Section = record.Section;

        var kindVMs = new List<RoosterCompatEggKindViewModel>();
        foreach (var kind in record.Kinds)
        {
            kindVMs.Add(new RoosterCompatEggKindViewModel(kind));
        }
        Kinds = kindVMs;
    }

    public string Category { get; }
    public string Section { get; }
    public string ButtonText => Category;
    public string MetaText => $"{Kinds.Count} kinds";

    public IReadOnlyList<RoosterCompatEggKindViewModel> Kinds { get; }

    public bool IsSelected => Kinds.Any(k => k.IsSelected);

    public void SelectEggGroup() => _onSelect(this);
}

internal sealed class RoosterCompatEggKindViewModel : INotifyPropertyChanged
{
    private string? _selectedSourceId;
    private readonly string? _defaultSourceId;

    public event PropertyChangedEventHandler? PropertyChanged;

    public RoosterCompatEggKindViewModel(RoosterCompatEggKindRecord record)
    {
        Kind = record.Kind;
        DisplayName = record.DisplayName;
        AvailableSources = record.AvailableSources?.ToList() ?? new List<string>();
        _selectedSourceId = record.SelectedSourceId;
        _defaultSourceId = AvailableSources.FirstOrDefault();
        ValidationNote = record.ValidationNote ?? string.Empty;
    }

    public EggKind Kind { get; }
    public string DisplayName { get; }
    public IReadOnlyList<string> AvailableSources { get; }
    public string ValidationNote { get; }

    public string? SelectedSourceId
    {
        get => _selectedSourceId;
        set
        {
            if (_selectedSourceId == value)
                return;
            _selectedSourceId = value;
            OnPropertyChanged(nameof(SelectedSourceId));
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    public bool IsSelected => !string.IsNullOrWhiteSpace(_selectedSourceId);

    public void ResetToDefault()
    {
        SelectedSourceId = _defaultSourceId;
    }

    private void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

internal sealed class RoosterCompatProviderViewModel
{
    private readonly Action<RoosterCompatProviderViewModel> _onSelect;

    public RoosterCompatProviderViewModel(RoosterCompatProviderRecord record, Action<RoosterCompatProviderViewModel> onSelect)
    {
        _onSelect = onSelect;
        ProviderId = record.ProviderId;
        DisplayName = record.DisplayName;
        IsEnabled = record.IsEnabled;
    }

    public string ProviderId { get; }
    public string DisplayName { get; }
    public bool IsEnabled { get; }

    public string ButtonText => DisplayName;
    public string DescriptionText => IsEnabled ? "Enabled" : "Disabled";

    public void SelectProvider() => _onSelect(this);
}
