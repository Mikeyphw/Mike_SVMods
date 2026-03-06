using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using RoosterControl.Framework.Config;

namespace RoosterControl.Integrations.StardewUi.Females;

internal sealed class RoosterFemalesEditorViewModel : INotifyPropertyChanged
{
    private readonly RoosterFemalesEditorService _service;
    private readonly RoosterControlConfig _config;
    private readonly Func<RoosterFemalesEditorSnapshot> _loadSnapshot;
    private readonly Action _saveChanges;

    private RoosterFemalesEditorSnapshot _snapshot;
    private RoosterFemalesEditorDraft _draft;
    private string _searchText = string.Empty;
    private string? _selectedFemaleKey;
    private bool _hasChanges;

    public event PropertyChangedEventHandler? PropertyChanged;

    public RoosterFemalesEditorViewModel(
        RoosterFemalesEditorService service,
        RoosterControlConfig config,
        Func<RoosterFemalesEditorSnapshot> loadSnapshot,
        Action saveChanges)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _loadSnapshot = loadSnapshot ?? throw new ArgumentNullException(nameof(loadSnapshot));
        _saveChanges = saveChanges ?? throw new ArgumentNullException(nameof(saveChanges));

        _snapshot = _loadSnapshot();
        _draft = _service.CreateDraft(_snapshot);
        RebuildViewState();
    }

    public string Title => "Females Editor";

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (string.Equals(_searchText, value, StringComparison.Ordinal))
                return;

            _searchText = value ?? string.Empty;
            RebuildViewState();
            OnPropertyChanged(nameof(SearchText));
        }
    }

    public string SummaryText { get; private set; } = string.Empty;

    public IReadOnlyList<RoosterFemalesFemaleItemViewModel> Females { get; private set; } = Array.Empty<RoosterFemalesFemaleItemViewModel>();

    public string SelectedFemale => SelectedFemaleItem?.ButtonText ?? string.Empty;

    public bool HasSelectedFemale => SelectedFemaleItem is not null;

    public IReadOnlyList<RoosterFemalesAssignmentOptionViewModel> AssignmentOptions { get; private set; } = Array.Empty<RoosterFemalesAssignmentOptionViewModel>();

    public bool CanSave => _hasChanges;

    public bool CanDiscard => _hasChanges;

    private RoosterFemalesFemaleItemViewModel? SelectedFemaleItem { get; set; }

    public void OpenFemale(RoosterFemalesFemaleItemViewModel female)
    {
        ArgumentNullException.ThrowIfNull(female);
        _selectedFemaleKey = female.FemaleKey;
        RebuildSelectionState();
    }

    public void Save()
    {
        if (!_hasChanges)
            return;

        _service.ApplyDraft(_config, _draft);
        _saveChanges();
        _snapshot = _loadSnapshot();
        _draft = _service.CreateDraft(_snapshot);
        _hasChanges = false;
        RebuildViewState();
    }

    public void Discard()
    {
        _draft = _service.CreateDraft(_snapshot);
        _hasChanges = false;
        RebuildViewState();
    }

    public void Reload()
    {
        _snapshot = _loadSnapshot();
        _draft = _service.CreateDraft(_snapshot);
        _hasChanges = false;
        RebuildViewState();
    }

    private void SelectAssignment(RoosterFemalesAssignmentOptionViewModel option)
    {
        ArgumentNullException.ThrowIfNull(option);

        if (SelectedFemaleItem is null)
            return;

        var assignments = new Dictionary<string, FemaleRoosterAssignment>(_draft.Assignments, StringComparer.OrdinalIgnoreCase)
        {
            [SelectedFemaleItem.FemaleKey] = option.Value
        };

        _draft = new RoosterFemalesEditorDraft(assignments);
        _hasChanges = !MatchesSnapshot();
        RebuildViewState();
    }

    private void RebuildViewState()
    {
        var filteredRecords = _snapshot.Females
            .Where(MatchesSearch)
            .ToList();

        Females = filteredRecords
            .Select(record => new RoosterFemalesFemaleItemViewModel(
                record.FemaleKey,
                record.Label,
                BuildMetaText(record),
                record.TooltipText,
                OpenFemale))
            .ToList();

        SummaryText = BuildSummaryText(filteredRecords.Count);
        RebuildSelectionState();

        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(Females));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
    }

    private void RebuildSelectionState()
    {
        if (Females.Count == 0)
        {
            SelectedFemaleItem = null;
            AssignmentOptions = Array.Empty<RoosterFemalesAssignmentOptionViewModel>();
        }
        else
        {
            SelectedFemaleItem = Females.FirstOrDefault(f => string.Equals(f.FemaleKey, _selectedFemaleKey, StringComparison.OrdinalIgnoreCase))
                ?? Females[0];
            _selectedFemaleKey = SelectedFemaleItem.FemaleKey;

            var selectedRecord = _snapshot.Females.First(f => string.Equals(f.FemaleKey, _selectedFemaleKey, StringComparison.OrdinalIgnoreCase));
            var selectedAssignment = _draft.Assignments.TryGetValue(_selectedFemaleKey, out var current)
                ? current
                : selectedRecord.CurrentAssignment;

            AssignmentOptions = selectedRecord.AssignmentOptions
                .Select(option => new RoosterFemalesAssignmentOptionViewModel(
                    option.Value,
                    option.ButtonText,
                    option.Value == selectedAssignment
                        ? "Current assignment"
                        : option.DescriptionText,
                    SelectAssignment))
                .ToList();
        }

        OnPropertyChanged(nameof(SelectedFemale));
        OnPropertyChanged(nameof(HasSelectedFemale));
        OnPropertyChanged(nameof(AssignmentOptions));
    }

    private string BuildMetaText(RoosterFemalesFemaleRecord record)
    {
        var assignment = _draft.Assignments.TryGetValue(record.FemaleKey, out var current)
            ? current
            : record.CurrentAssignment;
        return $"{assignment} rooster | {record.EggSummaryText}";
    }

    private string BuildSummaryText(int filteredCount)
        => filteredCount == _snapshot.Females.Count
            ? _snapshot.SummaryText
            : $"{_snapshot.SummaryText} | Visible: {filteredCount}";

    private bool MatchesSearch(RoosterFemalesFemaleRecord record)
    {
        if (string.IsNullOrWhiteSpace(_searchText))
            return true;

        return record.Label.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || record.FemaleKey.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesSnapshot()
    {
        foreach (var female in _snapshot.Females)
        {
            var draftValue = _draft.Assignments.TryGetValue(female.FemaleKey, out var current)
                ? current
                : female.CurrentAssignment;

            if (draftValue != female.CurrentAssignment)
                return false;
        }

        return true;
    }

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

internal sealed class RoosterFemalesFemaleItemViewModel
{
    private readonly Action<RoosterFemalesFemaleItemViewModel> _onOpen;

    public RoosterFemalesFemaleItemViewModel(
        string femaleKey,
        string buttonText,
        string metaText,
        string tooltipText,
        Action<RoosterFemalesFemaleItemViewModel> onOpen)
    {
        FemaleKey = femaleKey;
        ButtonText = buttonText;
        MetaText = metaText;
        TooltipText = tooltipText;
        _onOpen = onOpen;
    }

    public string FemaleKey { get; }
    public string ButtonText { get; }
    public string MetaText { get; }
    public string TooltipText { get; }

    public void OpenFemale() => _onOpen(this);
}

internal sealed class RoosterFemalesAssignmentOptionViewModel
{
    private readonly Action<RoosterFemalesAssignmentOptionViewModel> _onSelect;

    public RoosterFemalesAssignmentOptionViewModel(
        FemaleRoosterAssignment value,
        string buttonText,
        string descriptionText,
        Action<RoosterFemalesAssignmentOptionViewModel> onSelect)
    {
        Value = value;
        ButtonText = buttonText;
        DescriptionText = descriptionText;
        _onSelect = onSelect;
    }

    public FemaleRoosterAssignment Value { get; }
    public string ButtonText { get; }
    public string DescriptionText { get; }

    public void SelectAssignment() => _onSelect(this);
}
