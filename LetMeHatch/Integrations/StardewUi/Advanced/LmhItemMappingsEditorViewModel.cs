using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LetMeHatch.Integrations.StardewUi.Advanced;

public sealed class LmhItemMappingsEditorViewModel : INotifyPropertyChanged
{
    private readonly LmhItemMappingsEditorService _service;
    private readonly Func<LmhItemMappingsEditorCatalog> _loadCatalog;
    private readonly Action<LmhItemMappingsEditorDraft> _saveDraft;
    private readonly Func<string, Tuple<Texture2D, Rectangle>?>? _loadAnimalPreviewSprite;
    private readonly Func<string, Tuple<Texture2D, Rectangle>?>? _loadItemPreviewSprite;

    private LmhItemMappingsEditorCatalog _catalog = new(Array.Empty<LmhItemMappingsGroupRecord>());
    private LmhItemMappingsEditorDraft _draft = new(new Dictionary<LetMeHatch.EnumsAndStrings.Enums.CloneItemType, Dictionary<string, LmhItemMappingDraftRow>>());
    private LetMeHatch.EnumsAndStrings.Enums.CloneItemType? _selectedGroup;
    private string? _selectedAnimalKey;
    private string _normalOverrideText = string.Empty;
    private string _largeOverrideText = string.Empty;
    private bool _isBrowsing = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LmhItemMappingsEditorViewModel(
        LmhItemMappingsEditorService service,
        Func<LmhItemMappingsEditorCatalog> loadCatalog,
        Action<LmhItemMappingsEditorDraft> saveDraft,
        Func<string, Tuple<Texture2D, Rectangle>?>? loadAnimalPreviewSprite = null,
        Func<string, Tuple<Texture2D, Rectangle>?>? loadItemPreviewSprite = null)
    {
        _service = service;
        _loadCatalog = loadCatalog;
        _saveDraft = saveDraft;
        _loadAnimalPreviewSprite = loadAnimalPreviewSprite;
        _loadItemPreviewSprite = loadItemPreviewSprite;
        Reload();
    }

    public string Title => "Item Mappings Editor";
    public string SelectedGroup => SelectedGroupRecord?.DisplayName ?? string.Empty;
    public string SelectedMapping => SelectedMappingRecord?.DisplayName ?? string.Empty;
    public bool HasSelectedMapping => SelectedMappingRecord is not null;
    public bool IsBrowsing => _isBrowsing;
    public bool IsShowingDetail => !_isBrowsing && SelectedMappingRecord is not null;
    public string SummaryText
    {
        get
        {
            if (SelectedMappingRecord is null)
                return $"Groups: {MappingGroups.Count} | Selected group: {SelectedGroup}";

            return $"{SelectedMappingRecord.DisplayName} | Effective normal: {EffectiveNormalItemText} | Effective large: {EffectiveLargeItemText}";
        }
    }

    public IReadOnlyList<LmhItemMappingsGroupItemViewModel> MappingGroups { get; private set; } = Array.Empty<LmhItemMappingsGroupItemViewModel>();
    public IReadOnlyList<LmhItemMappingItemViewModel> CurrentMappings { get; private set; } = Array.Empty<LmhItemMappingItemViewModel>();
    public bool CanSave => HasSelectedMapping && HasChanges;
    public bool CanDiscard => HasSelectedMapping && HasChanges;
    public bool CanOpenPreviousMapping => GetSelectedMappingIndex() > 0;
    public bool CanOpenNextMapping => GetSelectedMappingIndex() >= 0 && GetSelectedMappingIndex() < CurrentMappings.Count - 1;
    public string BaseNormalItemText => string.IsNullOrWhiteSpace(SelectedMappingRecord?.BaseNormalItemId) ? "(none)" : SelectedMappingRecord.BaseNormalItemId;
    public string BaseLargeItemText => string.IsNullOrWhiteSpace(SelectedMappingRecord?.BaseLargeItemId) ? "(none)" : SelectedMappingRecord.BaseLargeItemId;
    public string EffectiveNormalItemText
    {
        get
        {
            if (SelectedMappingRecord is null)
                return string.Empty;
            return string.IsNullOrWhiteSpace(NormalOverrideText) ? BaseNormalItemText : NormalOverrideText;
        }
    }
    public string EffectiveLargeItemText
    {
        get
        {
            if (SelectedMappingRecord is null)
                return string.Empty;
            return string.IsNullOrWhiteSpace(LargeOverrideText) ? BaseLargeItemText : LargeOverrideText;
        }
    }
    public string SelectionHelpText => HasSelectedMapping
        ? "Leave an override blank to fall back to the base saved mapping."
        : "Select a mapping to edit overrides.";
    public string NormalOverrideText
    {
        get => _normalOverrideText;
        set
        {
            if (_normalOverrideText == value)
                return;
            _normalOverrideText = value;
            NotifySelectionDetails();
        }
    }

    public string LargeOverrideText
    {
        get => _largeOverrideText;
        set
        {
            if (_largeOverrideText == value)
                return;
            _largeOverrideText = value;
            NotifySelectionDetails();
        }
    }

    private LmhItemMappingsGroupRecord? SelectedGroupRecord => _selectedGroup is not null ? _catalog.Groups.FirstOrDefault(x => x.CloneType == _selectedGroup) : null;
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
                    if (!string.Equals(current.NormalItemId, row.ConfiguredNormalItemId, StringComparison.Ordinal))
                        return true;
                    if (!string.Equals(current.LargeItemId, row.ConfiguredLargeItemId, StringComparison.Ordinal))
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

    public void BackToBrowser()
    {
        if (_isBrowsing)
            return;

        _isBrowsing = true;
        NotifyAll();
    }

    public void OpenPreviousMapping() => OpenAdjacentMapping(-1);

    public void OpenNextMapping() => OpenAdjacentMapping(1);

    public void Reload()
    {
        _catalog = _loadCatalog();
        _draft = _service.CreateDraft(_catalog);
        _selectedGroup ??= _catalog.Groups.FirstOrDefault()?.CloneType;
        if (_selectedAnimalKey is not null && SelectedGroupRecord?.Mappings.Any(x => x.AnimalKey == _selectedAnimalKey) != true)
            _selectedAnimalKey = null;
        SyncSelectedOverrides();
        RebuildGroups();
        NotifyAll();
    }

    public void ResetMapping()
    {
        if (SelectedGroupRecord is null || SelectedMappingRecord is null)
            return;

        var groupMap = new Dictionary<string, LmhItemMappingDraftRow>(_draft.Mappings[SelectedGroupRecord.CloneType], StringComparer.OrdinalIgnoreCase)
        {
            [SelectedMappingRecord.AnimalKey] = new LmhItemMappingDraftRow(SelectedMappingRecord.ConfiguredNormalItemId, SelectedMappingRecord.ConfiguredLargeItemId)
        };
        var draftMap = new Dictionary<LetMeHatch.EnumsAndStrings.Enums.CloneItemType, Dictionary<string, LmhItemMappingDraftRow>>(_draft.Mappings)
        {
            [SelectedGroupRecord.CloneType] = groupMap
        };
        _draft = _draft with { Mappings = draftMap };
        SyncSelectedOverrides();
        RebuildGroups();
        NotifyAll();
    }

    private void SyncSelectedOverrides()
    {
        if (SelectedGroupRecord is null || SelectedMappingRecord is null || !_draft.Mappings.TryGetValue(SelectedGroupRecord.CloneType, out var byAnimal) || !byAnimal.TryGetValue(SelectedMappingRecord.AnimalKey, out var row))
        {
            _normalOverrideText = string.Empty;
            _largeOverrideText = string.Empty;
            return;
        }

        _normalOverrideText = row.NormalItemId;
        _largeOverrideText = row.LargeItemId;
    }

    private void UpdateSelectedDraftRow()
    {
        if (SelectedGroupRecord is null || SelectedMappingRecord is null)
            return;

        var groupMap = new Dictionary<string, LmhItemMappingDraftRow>(_draft.Mappings[SelectedGroupRecord.CloneType], StringComparer.OrdinalIgnoreCase)
        {
            [SelectedMappingRecord.AnimalKey] = new LmhItemMappingDraftRow(NormalOverrideText ?? string.Empty, LargeOverrideText ?? string.Empty)
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
                group.CloneType,
                group.DisplayName,
                $"{group.Mappings.Count} mappings",
                () =>
                {
                    _selectedGroup = group.CloneType;
                    _selectedAnimalKey = null;
                    _isBrowsing = true;
                    SyncSelectedOverrides();
                    RebuildCurrentMappings();
                    NotifyAll();
                }))
            .ToList();

        RebuildCurrentMappings();
    }

    private void RebuildCurrentMappings()
    {
        if (SelectedGroupRecord is null)
        {
            CurrentMappings = Array.Empty<LmhItemMappingItemViewModel>();
            return;
        }

        CurrentMappings = SelectedGroupRecord.Mappings
            .Select(row => new LmhItemMappingItemViewModel(
                row.AnimalKey,
                row.DisplayName,
                $"Effective normal: {FormatValue(row.EffectiveNormalItemId)} | Effective large: {FormatValue(row.EffectiveLargeItemId)}",
                _loadAnimalPreviewSprite?.Invoke(row.AnimalKey),
                _loadItemPreviewSprite?.Invoke(ChoosePreviewItemId(row) ?? string.Empty),
                _loadItemPreviewSprite?.Invoke(row.EffectiveNormalItemId ?? string.Empty),
                _loadItemPreviewSprite?.Invoke(row.EffectiveLargeItemId ?? string.Empty),
                () =>
                {
                    _selectedAnimalKey = row.AnimalKey;
                    _isBrowsing = false;
                    SyncSelectedOverrides();
                    NotifyAll();
                }))
            .ToList();
    }

    private void NotifySelectionDetails()
    {
        OnPropertyChanged(nameof(NormalOverrideText));
        OnPropertyChanged(nameof(LargeOverrideText));
        OnPropertyChanged(nameof(EffectiveNormalItemText));
        OnPropertyChanged(nameof(EffectiveLargeItemText));
        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
    }

    private void NotifyAll()
    {
        OnPropertyChanged(nameof(SelectedGroup));
        OnPropertyChanged(nameof(SelectedMapping));
        OnPropertyChanged(nameof(HasSelectedMapping));
        OnPropertyChanged(nameof(IsBrowsing));
        OnPropertyChanged(nameof(IsShowingDetail));
        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(MappingGroups));
        OnPropertyChanged(nameof(CurrentMappings));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
        OnPropertyChanged(nameof(CanOpenPreviousMapping));
        OnPropertyChanged(nameof(CanOpenNextMapping));
        OnPropertyChanged(nameof(BaseNormalItemText));
        OnPropertyChanged(nameof(BaseLargeItemText));
        OnPropertyChanged(nameof(EffectiveNormalItemText));
        OnPropertyChanged(nameof(EffectiveLargeItemText));
        OnPropertyChanged(nameof(SelectionHelpText));
        OnPropertyChanged(nameof(NormalOverrideText));
        OnPropertyChanged(nameof(LargeOverrideText));
    }

    private static string FormatValue(string value)
        => string.IsNullOrWhiteSpace(value) ? "(none)" : value;

    private static string? ChoosePreviewItemId(LmhItemMappingRecord row)
        => FirstNonBlank(row.EffectiveNormalItemId, row.BaseNormalItemId, row.EffectiveLargeItemId, row.BaseLargeItemId);

    private static string? FirstNonBlank(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private int GetSelectedMappingIndex()
    {
        if (SelectedMappingRecord is null)
            return -1;

        for (var i = 0; i < CurrentMappings.Count; i++)
        {
            if (string.Equals(CurrentMappings[i].AnimalKey, SelectedMappingRecord.AnimalKey, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private void OpenAdjacentMapping(int delta)
    {
        var currentIndex = GetSelectedMappingIndex();
        if (currentIndex < 0)
            return;

        var nextIndex = currentIndex + delta;
        if (nextIndex < 0 || nextIndex >= CurrentMappings.Count)
            return;

        _selectedAnimalKey = CurrentMappings[nextIndex].AnimalKey;
        _isBrowsing = false;
        SyncSelectedOverrides();
        NotifyAll();
    }

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class LmhItemMappingsGroupItemViewModel
{
    private readonly Action _openGroup;

    public LmhItemMappingsGroupItemViewModel(LetMeHatch.EnumsAndStrings.Enums.CloneItemType cloneType, string buttonText, string metaText, Action openGroup)
    {
        CloneType = cloneType;
        ButtonText = buttonText;
        MetaText = metaText;
        _openGroup = openGroup;
    }

    public LetMeHatch.EnumsAndStrings.Enums.CloneItemType CloneType { get; }
    public string ButtonText { get; }
    public string MetaText { get; }

    public void OpenGroup() => _openGroup();
}

public sealed class LmhItemMappingItemViewModel
{
    private readonly Action _open;

    public LmhItemMappingItemViewModel(
        string animalKey,
        string buttonText,
        string metaText,
        Tuple<Texture2D, Rectangle>? animalPreviewSprite,
        Tuple<Texture2D, Rectangle>? previewSprite,
        Tuple<Texture2D, Rectangle>? normalProducePreviewSprite,
        Tuple<Texture2D, Rectangle>? largeProducePreviewSprite,
        Action open)
    {
        AnimalKey = animalKey;
        ButtonText = buttonText;
        MetaText = metaText;
        AnimalPreviewSprite = animalPreviewSprite;
        PreviewSprite = previewSprite;
        NormalProducePreviewSprite = normalProducePreviewSprite;
        LargeProducePreviewSprite = largeProducePreviewSprite;
        _open = open;
    }

    public string AnimalKey { get; }
    public string ButtonText { get; }
    public string MetaText { get; }
    public Tuple<Texture2D, Rectangle>? AnimalPreviewSprite { get; }
    public bool HasAnimalPreviewSprite => AnimalPreviewSprite is not null;
    public Tuple<Texture2D, Rectangle>? PreviewSprite { get; }
    public bool HasPreviewSprite => PreviewSprite is not null;
    public bool HasFallbackPreviewSprite => !HasAnimalPreviewSprite && HasPreviewSprite;
    public bool MissingAllPreviewSprites => !HasAnimalPreviewSprite && !HasPreviewSprite;
    public bool MissingPreviewSprite => !HasPreviewSprite;
    public Tuple<Texture2D, Rectangle>? NormalProducePreviewSprite { get; }
    public bool HasNormalProducePreviewSprite => NormalProducePreviewSprite is not null;
    public Tuple<Texture2D, Rectangle>? LargeProducePreviewSprite { get; }
    public bool HasLargeProducePreviewSprite => LargeProducePreviewSprite is not null;
    public string TooltipText => $"{ButtonText}\n{MetaText}";

    public void OpenMapping() => _open();
}
