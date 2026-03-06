using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace LetMeHatch.Integrations.StardewUi.Advanced;

public sealed class LmhItemMappingsEditorViewModel : INotifyPropertyChanged
{
    private readonly LmhItemMappingsEditorService _service;
    private readonly Func<LmhItemMappingsEditorCatalog> _loadCatalog;
    private readonly Action<LmhItemMappingsEditorDraft> _saveDraft;

    private LmhItemMappingsEditorCatalog _catalog = new(Array.Empty<LmhItemMappingsGroupRecord>());
    private LmhItemMappingsEditorDraft _draft = new(new Dictionary<LetMeHatch.EnumsAndStrings.Enums.CloneItemType, Dictionary<string, LmhItemMappingDraftRow>>());
    private string? _selectedGroupName;
    private string? _selectedAnimalKey;
    private string _normalItemText = string.Empty;
    private string _largeItemText = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LmhItemMappingsEditorViewModel(
        LmhItemMappingsEditorService service,
        Func<LmhItemMappingsEditorCatalog> loadCatalog,
        Action<LmhItemMappingsEditorDraft> saveDraft)
    {
        _service = service;
        _loadCatalog = loadCatalog;
        _saveDraft = saveDraft;
        Reload();
    }

    public string Title => "Item Mappings Editor";
    public string SelectedGroup => SelectedGroupRecord?.DisplayName ?? string.Empty;
    public string SelectedMapping => SelectedMappingRecord?.AnimalKey ?? string.Empty;
    public bool HasSelectedMapping => SelectedMappingRecord is not null;
    public string ComparisonText
    {
        get
        {
            if (SelectedMappingRecord is null)
                return $"Groups: {MappingGroups.Count}";

            return $"{SelectedMappingRecord.AnimalKey} | Normal: {NormalItemText} | Large: {LargeItemText}";
        }
    }

    public IReadOnlyList<LmhItemMappingsGroupItemViewModel> MappingGroups { get; private set; } = Array.Empty<LmhItemMappingsGroupItemViewModel>();
    public bool CanSave => HasSelectedMapping && HasChanges;
    public bool CanDiscard => HasSelectedMapping && HasChanges;
    public string NormalItemText
    {
        get => _normalItemText;
        set
        {
            if (_normalItemText == value)
                return;
            _normalItemText = value;
            OnPropertyChanged(nameof(NormalItemText));
            OnPropertyChanged(nameof(ComparisonText));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(CanDiscard));
        }
    }

    public string LargeItemText
    {
        get => _largeItemText;
        set
        {
            if (_largeItemText == value)
                return;
            _largeItemText = value;
            OnPropertyChanged(nameof(LargeItemText));
            OnPropertyChanged(nameof(ComparisonText));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(CanDiscard));
        }
    }

    private LmhItemMappingsGroupRecord? SelectedGroupRecord => _catalog.Groups.FirstOrDefault(x => x.DisplayName == _selectedGroupName);
    private LmhItemMappingRecord? SelectedMappingRecord => SelectedGroupRecord?.Mappings.FirstOrDefault(x => x.AnimalKey == _selectedAnimalKey);

    private bool HasChanges
    {
        get
        {
            foreach (var group in _catalog.Groups)
            {
                if (!_draft.Mappings.TryGetValue(group.CloneType, out var byAnimal))
                    return true;

                foreach (var row in group.Mappings)
                {
                    if (!byAnimal.TryGetValue(row.AnimalKey, out var current))
                        return true;
                    if (!string.Equals(current.NormalItemId, row.NormalItemId, StringComparison.Ordinal))
                        return true;
                    if (!string.Equals(current.LargeItemId, row.LargeItemId, StringComparison.Ordinal))
                        return true;
                }
            }

            return false;
        }
    }

    public void Save()
    {
        if (!HasSelectedMapping || SelectedGroupRecord is null || SelectedMappingRecord is null)
            return;

        UpdateSelectedDraftRow();
        _saveDraft(_draft);
        Reload();
    }

    public void Discard()
        => Reload();

    public void Reload()
    {
        _catalog = _loadCatalog();
        _draft = _service.CreateDraft(_catalog);
        _selectedGroupName ??= _catalog.Groups.FirstOrDefault()?.DisplayName;
        _selectedAnimalKey ??= SelectedGroupRecord?.Mappings.FirstOrDefault()?.AnimalKey;
        SyncSelectedTexts();
        RebuildGroups();
        NotifyAll();
    }

    public void ResetMapping()
    {
        if (SelectedGroupRecord is null || SelectedMappingRecord is null)
            return;

        var groupMap = new Dictionary<string, LmhItemMappingDraftRow>(_draft.Mappings[SelectedGroupRecord.CloneType], StringComparer.OrdinalIgnoreCase)
        {
            [SelectedMappingRecord.AnimalKey] = new LmhItemMappingDraftRow(SelectedMappingRecord.NormalItemId, SelectedMappingRecord.LargeItemId)
        };
        var draftMap = new Dictionary<LetMeHatch.EnumsAndStrings.Enums.CloneItemType, Dictionary<string, LmhItemMappingDraftRow>>(_draft.Mappings)
        {
            [SelectedGroupRecord.CloneType] = groupMap
        };
        _draft = _draft with { Mappings = draftMap };
        SyncSelectedTexts();
        RebuildGroups();
        NotifyAll();
    }

    private void SyncSelectedTexts()
    {
        if (SelectedGroupRecord is null || SelectedMappingRecord is null || !_draft.Mappings.TryGetValue(SelectedGroupRecord.CloneType, out var byAnimal) || !byAnimal.TryGetValue(SelectedMappingRecord.AnimalKey, out var row))
        {
            NormalItemText = string.Empty;
            LargeItemText = string.Empty;
            return;
        }

        NormalItemText = row.NormalItemId;
        LargeItemText = row.LargeItemId;
    }

    private void UpdateSelectedDraftRow()
    {
        if (SelectedGroupRecord is null || SelectedMappingRecord is null)
            return;

        var groupMap = new Dictionary<string, LmhItemMappingDraftRow>(_draft.Mappings[SelectedGroupRecord.CloneType], StringComparer.OrdinalIgnoreCase)
        {
            [SelectedMappingRecord.AnimalKey] = new LmhItemMappingDraftRow(NormalItemText ?? string.Empty, LargeItemText ?? string.Empty)
        };
        var draftMap = new Dictionary<LetMeHatch.EnumsAndStrings.Enums.CloneItemType, Dictionary<string, LmhItemMappingDraftRow>>(_draft.Mappings)
        {
            [SelectedGroupRecord.CloneType] = groupMap
        };
        _draft = _draft with { Mappings = draftMap };
    }

    private void RebuildGroups()
    {
        MappingGroups = _catalog.Groups
            .Select(group => new LmhItemMappingsGroupItemViewModel(
                group.DisplayName,
                $"{group.Mappings.Count} mappings",
                "Open mapping",
                () =>
                {
                    _selectedGroupName = group.DisplayName;
                    _selectedAnimalKey ??= group.Mappings.FirstOrDefault()?.AnimalKey;
                    SyncSelectedTexts();
                    NotifyAll();
                },
                () =>
                {
                    _selectedGroupName = group.DisplayName;
                    _selectedAnimalKey = group.Mappings.FirstOrDefault()?.AnimalKey;
                    SyncSelectedTexts();
                    NotifyAll();
                }))
            .ToList();
    }

    private void NotifyAll()
    {
        OnPropertyChanged(nameof(SelectedGroup));
        OnPropertyChanged(nameof(SelectedMapping));
        OnPropertyChanged(nameof(HasSelectedMapping));
        OnPropertyChanged(nameof(ComparisonText));
        OnPropertyChanged(nameof(MappingGroups));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
        OnPropertyChanged(nameof(NormalItemText));
        OnPropertyChanged(nameof(LargeItemText));
    }

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class LmhItemMappingsGroupItemViewModel
{
    private readonly Action _openGroup;
    private readonly Action _openMapping;

    public LmhItemMappingsGroupItemViewModel(string buttonText, string metaText, string openText, Action openGroup, Action openMapping)
    {
        ButtonText = buttonText;
        MetaText = metaText;
        OpenText = openText;
        _openGroup = openGroup;
        _openMapping = openMapping;
    }

    public string ButtonText { get; }
    public string MetaText { get; }
    public string OpenText { get; }
    public string StateText => MetaText;

    public void OpenGroup() => _openGroup();
    public void OpenMapping() => _openMapping();
}
