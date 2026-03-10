using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using RoosterControl.Constants;
using RoosterControl.Enums;
using RoosterControl.Framework.Config;

namespace RoosterControl.Integrations.StardewUi.Produce;

internal sealed class RoosterProduceEditorViewModel : INotifyPropertyChanged
{
    private readonly RoosterProduceEditorService _service;
    private readonly RoosterControlConfig _config;
    private readonly Action _saveChanges;
    private readonly Func<string, Tuple<Texture2D, Rectangle>?>? _loadRoosterPreviewSprite;
    private readonly Func<string, Tuple<Texture2D, Rectangle>?>? _loadObjectPreviewSprite;
    private readonly IReadOnlyList<RoosterProduceExistingItemOptionRecord> _existingItemOptions;
    private RoosterProduceEditorSnapshot _snapshot;
    private RoosterProduceEditorDraft _draft;
    private RoosterType _selectedRoosterType;
    private RoosterProduceSection _selectedSection;
    private bool _hasChanges;

    public event PropertyChangedEventHandler? PropertyChanged;

    public RoosterProduceEditorViewModel(
        RoosterProduceEditorService service,
        RoosterControlConfig config,
        Action saveChanges,
        RoosterType initialRoosterType,
        RoosterProduceSection initialSection,
        Func<string, Tuple<Texture2D, Rectangle>?>? loadRoosterPreviewSprite = null,
        Func<string, Tuple<Texture2D, Rectangle>?>? loadObjectPreviewSprite = null)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _saveChanges = saveChanges ?? throw new ArgumentNullException(nameof(saveChanges));
        _loadRoosterPreviewSprite = loadRoosterPreviewSprite;
        _loadObjectPreviewSprite = loadObjectPreviewSprite;
        _snapshot = _service.LoadSnapshot(_config);
        _draft = _service.CreateDraft(_snapshot);
        _existingItemOptions = _service.GetExistingItemOptions();
        _selectedRoosterType = _snapshot.Roosters.Any(r => r.Type == initialRoosterType)
            ? initialRoosterType
            : _snapshot.Roosters.First().Type;
        _selectedSection = initialSection;
        RebuildState();
    }

    public string Title => "Produce Editor";

    public IReadOnlyList<RoosterProduceRoosterItemViewModel> Roosters { get; private set; } = Array.Empty<RoosterProduceRoosterItemViewModel>();

    public IReadOnlyList<string> ProduceModeOptions => _service.GetProduceModeOptions();

    public IReadOnlyList<string> DeluxeModeOptions => _service.GetDeluxeProduceModeOptions();

    public string SelectedRoosterName => CurrentRooster.DisplayName;
    public Tuple<Texture2D, Rectangle>? SelectedRoosterSprite => _loadRoosterPreviewSprite?.Invoke(CurrentRooster.Type.ToString());
    public bool HasSelectedRoosterSprite => SelectedRoosterSprite is not null;

    public string SummaryText => $"{CurrentRooster.DisplayName} rooster | {ModeSummaryText}";

    public string ModeSummaryText => _service.BuildModeSummary(CurrentRooster);

    public string ActiveSectionSummary => _service.BuildSectionSummary(CurrentRooster, _selectedSection);

    public bool IsNormalSection => _selectedSection == RoosterProduceSection.Normal;

    public bool IsDeluxeSection => _selectedSection == RoosterProduceSection.Deluxe;

    public string NormalTabText => IsNormalSection ? "[Normal Produce]" : "Normal Produce";

    public string DeluxeTabText => IsDeluxeSection ? "[Deluxe Produce]" : "Deluxe Produce";

    public string SelectedProduceMode
    {
        get => CurrentRooster.ProduceMode.ToString();
        set
        {
            if (!Enum.TryParse<ProduceMode>(value, true, out var parsed) || CurrentRooster.ProduceMode == parsed)
                return;

            CurrentRooster.ProduceMode = parsed;
            MarkDirty();
            OnPropertyChanged(nameof(SelectedProduceMode));
            OnPropertyChanged(nameof(ModeSummaryText));
            OnPropertyChanged(nameof(ActiveSectionSummary));
            OnPropertyChanged(nameof(SummaryText));
        }
    }

    public string SelectedDeluxeMode
    {
        get => CurrentRooster.DeluxeProduceMode.ToString();
        set
        {
            if (!Enum.TryParse<DeluxeProduceMode>(value, true, out var parsed) || CurrentRooster.DeluxeProduceMode == parsed)
                return;

            CurrentRooster.DeluxeProduceMode = parsed;
            MarkDirty();
            OnPropertyChanged(nameof(SelectedDeluxeMode));
            OnPropertyChanged(nameof(ModeSummaryText));
            OnPropertyChanged(nameof(ActiveSectionSummary));
            OnPropertyChanged(nameof(SummaryText));
        }
    }

    public IReadOnlyList<RoosterProduceSlotViewModel> CurrentSlots { get; private set; } = Array.Empty<RoosterProduceSlotViewModel>();

    public bool CanSave => _hasChanges;

    public bool CanDiscard => _hasChanges;

    private RoosterProduceRoosterDraft CurrentRooster => _draft.Roosters[_selectedRoosterType];

    public void OpenRooster(RoosterProduceRoosterItemViewModel rooster)
    {
        ArgumentNullException.ThrowIfNull(rooster);

        if (_selectedRoosterType == rooster.Type)
            return;

        _selectedRoosterType = rooster.Type;
        RebuildState();
    }

    public void ShowNormalSection()
    {
        if (_selectedSection == RoosterProduceSection.Normal)
            return;

        _selectedSection = RoosterProduceSection.Normal;
        RebuildSlots();
        RaiseSectionProperties();
    }

    public void ShowDeluxeSection()
    {
        if (_selectedSection == RoosterProduceSection.Deluxe)
            return;

        _selectedSection = RoosterProduceSection.Deluxe;
        RebuildSlots();
        RaiseSectionProperties();
    }

    public void Save()
    {
        if (!_hasChanges)
            return;

        _service.ApplyDraft(_config, _draft);
        _saveChanges();
        _snapshot = _service.LoadSnapshot(_config);
        _draft = _service.CreateDraft(_snapshot);
        _hasChanges = false;
        RebuildState();
    }

    public void Discard()
    {
        _draft = _service.CreateDraft(_snapshot);
        _hasChanges = false;
        RebuildState();
    }

    public void Reload()
    {
        _snapshot = _service.LoadSnapshot(_config);
        _draft = _service.CreateDraft(_snapshot);
        _hasChanges = false;
        RebuildState();
    }

    private void RebuildState()
    {
        Roosters = _draft.Roosters.Values
            .Select(rooster => new RoosterProduceRoosterItemViewModel(
                rooster.Type,
                rooster.DisplayName,
                $"{rooster.ProduceMode} | {rooster.DeluxeProduceMode}",
                OpenRooster))
            .ToList();

        RebuildSlots();

        OnPropertyChanged(nameof(Roosters));
        OnPropertyChanged(nameof(SelectedRoosterName));
        OnPropertyChanged(nameof(SelectedRoosterSprite));
        OnPropertyChanged(nameof(HasSelectedRoosterSprite));
        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(ModeSummaryText));
        OnPropertyChanged(nameof(SelectedProduceMode));
        OnPropertyChanged(nameof(SelectedDeluxeMode));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
        RaiseSectionProperties();
    }

    private void RebuildSlots()
    {
        var slots = (_selectedSection == RoosterProduceSection.Normal ? CurrentRooster.NormalSlots : CurrentRooster.DeluxeSlots)
            .Select(slot => new RoosterProduceSlotViewModel(
                slot,
                _snapshot.ChoiceOptions,
                _existingItemOptions,
                _service.ValidateExistingItem,
                MarkDirty,
                _loadObjectPreviewSprite))
            .ToList();

        CurrentSlots = slots;
        OnPropertyChanged(nameof(CurrentSlots));
    }

    private void RaiseSectionProperties()
    {
        OnPropertyChanged(nameof(IsNormalSection));
        OnPropertyChanged(nameof(IsDeluxeSection));
        OnPropertyChanged(nameof(NormalTabText));
        OnPropertyChanged(nameof(DeluxeTabText));
        OnPropertyChanged(nameof(ActiveSectionSummary));
    }

    private void MarkDirty()
    {
        _hasChanges = !MatchesSnapshot();
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
        OnPropertyChanged(nameof(SummaryText));
    }

    private bool MatchesSnapshot()
    {
        foreach (var rooster in _snapshot.Roosters)
        {
            if (!_draft.Roosters.TryGetValue(rooster.Type, out var draftRooster))
                return false;

            if (draftRooster.ProduceMode != rooster.ProduceMode || draftRooster.DeluxeProduceMode != rooster.DeluxeProduceMode)
                return false;

            if (!MatchesSection(draftRooster.NormalSlots, rooster.Normal.Slots) || !MatchesSection(draftRooster.DeluxeSlots, rooster.Deluxe.Slots))
                return false;
        }

        return true;
    }

    private static bool MatchesSection(IReadOnlyList<RoosterProduceSlotDraft> draftSlots, IReadOnlyList<RoosterProduceSlotRecord> snapshotSlots)
    {
        if (draftSlots.Count != snapshotSlots.Count)
            return false;

        for (var i = 0; i < draftSlots.Count; i++)
        {
            var draft = draftSlots[i];
            var snapshot = snapshotSlots[i];
            if (!string.Equals(draft.Choice, snapshot.Choice, StringComparison.Ordinal)
                || !string.Equals(draft.CustomItemName, snapshot.CustomItemName, StringComparison.Ordinal)
                || draft.Weight != snapshot.Weight)
                return false;
        }

        return true;
    }

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

internal sealed class RoosterProduceRoosterItemViewModel
{
    private readonly Action<RoosterProduceRoosterItemViewModel> _onOpen;

    public RoosterProduceRoosterItemViewModel(
        RoosterType type,
        string buttonText,
        string metaText,
        Action<RoosterProduceRoosterItemViewModel> onOpen)
    {
        Type = type;
        ButtonText = buttonText;
        MetaText = metaText;
        _onOpen = onOpen;
    }

    public RoosterType Type { get; }
    public string ButtonText { get; }
    public string MetaText { get; }

    public void OpenRooster() => _onOpen(this);
}

internal sealed class RoosterProduceSlotViewModel : INotifyPropertyChanged
{
    private const int ExistingItemPickerLimit = 120;

    private readonly RoosterProduceSlotDraft _draft;
    private readonly Action _onChanged;
    private readonly IReadOnlyList<RoosterProduceChoiceOptionRecord> _choiceOptions;
    private readonly IReadOnlyList<RoosterProduceExistingItemOptionRecord> _existingItemOptions;
    private readonly Func<string?, RoosterProduceItemValidationResult>? _validateCustomItem;
    private readonly Func<string, Tuple<Texture2D, Rectangle>?>? _loadObjectPreviewSprite;
    private string _existingItemSearchText = string.Empty;
    private bool _isPickingExistingItem;
    private string _validationText = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public RoosterProduceSlotViewModel(
        RoosterProduceSlotDraft draft,
        IReadOnlyList<RoosterProduceChoiceOptionRecord> choiceOptions,
        IReadOnlyList<RoosterProduceExistingItemOptionRecord> existingItemOptions,
        Func<string?, RoosterProduceItemValidationResult> validateCustomItem,
        Action onChanged,
        Func<string, Tuple<Texture2D, Rectangle>?>? loadObjectPreviewSprite = null)
    {
        _draft = draft;
        _choiceOptions = choiceOptions;
        _existingItemOptions = existingItemOptions;
        _validateCustomItem = validateCustomItem;
        _onChanged = onChanged;
        _loadObjectPreviewSprite = loadObjectPreviewSprite;
    }

    public string SlotTitle => $"Slot {_draft.Slot}";

    public IReadOnlyList<string> Choices => _choiceOptions.Select(option => option.Key).ToList();

    public string SelectedChoice
    {
        get => _draft.Choice;
        set
        {
            var next = string.IsNullOrWhiteSpace(value) ? ItemIds.CustomItems.Custom : value.Trim();
            if (string.Equals(_draft.Choice, next, StringComparison.Ordinal))
                return;

            _draft.Choice = next;
            UpdateDisplayMetadata();
            _onChanged();
            OnPropertyChanged(nameof(SelectedChoice));
            OnPropertyChanged(nameof(IsCustom));
            OnPropertyChanged(nameof(DisplayLabel));
            OnPropertyChanged(nameof(Category));
            OnPropertyChanged(nameof(StableKey));
            OnPropertyChanged(nameof(PreviewItemId));
            OnPropertyChanged(nameof(PreviewSprite));
            OnPropertyChanged(nameof(HasPreviewSprite));
        }
    }

    public bool IsCustom => string.Equals(_draft.Choice, ItemIds.CustomItems.Custom, StringComparison.OrdinalIgnoreCase);

    public string CustomItemName
    {
        get => _draft.CustomItemName;
        set
        {
            var next = string.IsNullOrWhiteSpace(value) ? ItemIds.CustomItems.Custom : value.Trim();
            if (string.Equals(_draft.CustomItemName, next, StringComparison.Ordinal))
                return;

            _draft.CustomItemName = next;
            _validationText = string.Empty;
            _onChanged();
            OnPropertyChanged(nameof(CustomItemName));
            OnPropertyChanged(nameof(PreviewItemId));
            OnPropertyChanged(nameof(PreviewSprite));
            OnPropertyChanged(nameof(HasPreviewSprite));
            OnPropertyChanged(nameof(ValidationText));
        }
    }

    public float Weight
    {
        get => _draft.Weight;
        set
        {
            var next = Math.Clamp((int)MathF.Round(value), 0, 100);
            if (_draft.Weight == next)
                return;

            _draft.Weight = next;
            _onChanged();
            OnPropertyChanged(nameof(Weight));
            OnPropertyChanged(nameof(WeightText));
        }
    }

    public string WeightText => _draft.Weight.ToString();

    public string DisplayLabel => _draft.DisplayLabel;

    public string Category => _draft.Category;

    public string StableKey => _draft.StableKey;
    public string PreviewItemId => IsCustom ? CustomItemName : StableKey;
    public Tuple<Texture2D, Rectangle>? PreviewSprite => _loadObjectPreviewSprite?.Invoke(PreviewItemId);
    public bool HasPreviewSprite => PreviewSprite is not null;
    public bool IsPickingExistingItem => _isPickingExistingItem;
    public string ExistingItemSearchText
    {
        get => _existingItemSearchText;
        set
        {
            var next = value ?? string.Empty;
            if (string.Equals(_existingItemSearchText, next, StringComparison.Ordinal))
                return;

            _existingItemSearchText = next;
            OnPropertyChanged(nameof(ExistingItemSearchText));
            OnPropertyChanged(nameof(VisibleExistingItems));
        }
    }
    public IReadOnlyList<RoosterProduceExistingItemTileViewModel> VisibleExistingItems
        => _existingItemOptions
            .Where(MatchesExistingItemSearch)
            .Take(ExistingItemPickerLimit)
            .Select(option => new RoosterProduceExistingItemTileViewModel(
                option.ItemId,
                option.DisplayText,
                _loadObjectPreviewSprite?.Invoke(option.ItemId),
                () => ApplyExistingItem(option.ItemId)))
            .ToList();

    public string ValidationText => _validationText;

    public void ValidateCustomItem()
    {
        if (_validateCustomItem is null)
            return;

        var result = _validateCustomItem(CustomItemName);
        if (result.IsValid)
            _draft.CustomItemName = result.NormalizedItemId;

        _validationText = result.Message;
        OnPropertyChanged(nameof(CustomItemName));
        OnPropertyChanged(nameof(PreviewItemId));
        OnPropertyChanged(nameof(PreviewSprite));
        OnPropertyChanged(nameof(HasPreviewSprite));
        OnPropertyChanged(nameof(ValidationText));
    }

    public void ShowExistingItemPicker()
    {
        if (_isPickingExistingItem)
            return;

        _isPickingExistingItem = true;
        OnPropertyChanged(nameof(IsPickingExistingItem));
        OnPropertyChanged(nameof(VisibleExistingItems));
    }

    public void HideExistingItemPicker()
    {
        if (!_isPickingExistingItem)
            return;

        _isPickingExistingItem = false;
        OnPropertyChanged(nameof(IsPickingExistingItem));
    }

    public void ApplyExistingItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return;

        CustomItemName = itemId;
        ValidateCustomItem();
        HideExistingItemPicker();
    }

    private void UpdateDisplayMetadata()
    {
        var option = _choiceOptions.FirstOrDefault(candidate => string.Equals(candidate.Key, _draft.Choice, StringComparison.OrdinalIgnoreCase));
        if (option is null)
            return;

        _draft.DisplayLabel = option.DisplayLabel;
        _draft.Category = option.Category;
        _draft.StableKey = option.StableKey;
    }

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool MatchesExistingItemSearch(RoosterProduceExistingItemOptionRecord option)
    {
        if (string.IsNullOrWhiteSpace(_existingItemSearchText))
            return true;

        return option.DisplayText.Contains(_existingItemSearchText, StringComparison.OrdinalIgnoreCase)
            || option.ItemId.Contains(_existingItemSearchText, StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class RoosterProduceExistingItemTileViewModel
{
    private readonly Action _onPick;

    public RoosterProduceExistingItemTileViewModel(
        string itemId,
        string tooltipText,
        Tuple<Texture2D, Rectangle>? previewSprite,
        Action onPick)
    {
        ItemId = itemId;
        TooltipText = tooltipText;
        PreviewSprite = previewSprite;
        _onPick = onPick;
    }

    public string ItemId { get; }
    public string TooltipText { get; }
    public Tuple<Texture2D, Rectangle>? PreviewSprite { get; }
    public bool HasPreviewSprite => PreviewSprite is not null;
    public bool MissingPreviewSprite => PreviewSprite is null;

    public void Pick() => _onPick();
}
