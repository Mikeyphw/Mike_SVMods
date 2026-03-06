using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mikew.NestCore.Helpers.UI;

namespace EggsInWild.Infrastructure.Integrations.StardewUi.WildAnimals;

public sealed class WildAnimalDraftViewModel : ObservableObject
{
    private readonly WildAnimalsEditorService _service;
    private const string OverviewTabKey = "overview";
    private const string LocationTypesTabKey = "locationTypes";
    private const string ModKeysTabKey = "modKeys";
    private const string WarningsTabKey = "warnings";

    private bool _enabled;
    private bool _largeEgg;
    private bool _isMagic;
    private string _priceText;
    private string _newModGroupId = "";
    private string _priceError = "";
    private string _headerStatus = "Ready";
    private Tuple<Texture2D, Rectangle>? _previewSprite;
    private string _previewStatus = "";
    private string _warningSummary = "";
    private bool _hasWarnings;
    private string _currentTab = OverviewTabKey;

    public WildAnimalDraftViewModel(WildAnimalsEditorService service, WildAnimalDraft draft)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        Draft = draft ?? throw new ArgumentNullException(nameof(draft));
        _enabled = draft.Enabled;
        _largeEgg = draft.LargeEgg;
        _isMagic = draft.IsMagic;
        _priceText = draft.Price.ToString();

        LocationTypes = draft.LocationTypes
            .Select(location => new WildAnimalLocationTypeEditorViewModel(location.Key, location.BaseAllowed, location.Allowed, RefreshState))
            .ToList();

        ModGroups = draft.ModGroups
            .Select(group => new WildAnimalModGroupEditorViewModel(group.ModId, group.BaseKeys, group.Keys, RefreshState, RemoveModGroup))
            .ToList();

        AddModGroupCommand = new RelayCommand(AddModGroupCore, CanAddModGroup);
        ResetEnabledCommand = new RelayCommand(() => Enabled = Draft.BaseEnabled, () => Enabled != Draft.BaseEnabled);
        ResetLargeEggCommand = new RelayCommand(() => LargeEgg = Draft.BaseLargeEgg, () => LargeEgg != Draft.BaseLargeEgg);
        ResetIsMagicCommand = new RelayCommand(() => IsMagic = Draft.BaseIsMagic, () => IsMagic != Draft.BaseIsMagic);
        ResetPriceCommand = new RelayCommand(() => PriceText = Draft.BasePrice.ToString(), () => PriceText != Draft.BasePrice.ToString());
        RefreshState();
    }

    public WildAnimalDraft Draft { get; }
    public string Key => Draft.Key;
    public IReadOnlyList<WildAnimalLocationTypeEditorViewModel> LocationTypes { get; }
    public List<WildAnimalModGroupEditorViewModel> ModGroups { get; }

    public RelayCommand AddModGroupCommand { get; }
    public RelayCommand ResetEnabledCommand { get; }
    public RelayCommand ResetLargeEggCommand { get; }
    public RelayCommand ResetIsMagicCommand { get; }
    public RelayCommand ResetPriceCommand { get; }

    public void AddModGroup() => AddModGroupCommand.Execute(null);
    public void ResetEnabled() => ResetEnabledCommand.Execute(null);
    public void ResetLargeEgg() => ResetLargeEggCommand.Execute(null);
    public void ResetIsMagic() => ResetIsMagicCommand.Execute(null);
    public void ResetPrice() => ResetPriceCommand.Execute(null);
    public void ShowOverviewTab() => SetTab(OverviewTabKey);
    public void ShowLocationTypesTab() => SetTab(LocationTypesTabKey);
    public void ShowModKeysTab() => SetTab(ModKeysTabKey);
    public void ShowWarningsTab() => SetTab(WarningsTabKey);

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (!SetProperty(ref _enabled, value))
                return;

            RefreshState();
        }
    }

    public bool LargeEgg
    {
        get => _largeEgg;
        set
        {
            if (!SetProperty(ref _largeEgg, value))
                return;

            RefreshState();
        }
    }

    public bool IsMagic
    {
        get => _isMagic;
        set
        {
            if (!SetProperty(ref _isMagic, value))
                return;

            RefreshState();
        }
    }

    public string PriceText
    {
        get => _priceText;
        set
        {
            if (!SetProperty(ref _priceText, value))
                return;

            RefreshState();
        }
    }

    public string NewModGroupId
    {
        get => _newModGroupId;
        set
        {
            if (!SetProperty(ref _newModGroupId, value))
                return;

            AddModGroupCommand.NotifyCanExecuteChanged();
        }
    }

    public string PriceError
    {
        get => _priceError;
        private set => SetProperty(ref _priceError, value);
    }

    public bool HasValidationErrors => !string.IsNullOrWhiteSpace(PriceError);
    public bool HasWarnings
    {
        get => _hasWarnings;
        private set => SetProperty(ref _hasWarnings, value);
    }

    public string HeaderStatus
    {
        get => _headerStatus;
        private set => SetProperty(ref _headerStatus, value);
    }

    public string PreviewStatus
    {
        get => _previewStatus;
        private set => SetProperty(ref _previewStatus, value);
    }

    public Tuple<Texture2D, Rectangle>? PreviewSprite
    {
        get => _previewSprite;
        private set => SetProperty(ref _previewSprite, value);
    }

    public bool HasPreviewSprite => PreviewSprite is not null;
    public bool MissingPreviewSprite => !HasPreviewSprite;

    public string WarningSummary
    {
        get => _warningSummary;
        private set => SetProperty(ref _warningSummary, value);
    }

    public string CurrentTab => _currentTab;
    public bool IsOverviewTab => string.Equals(CurrentTab, OverviewTabKey, StringComparison.Ordinal);
    public bool IsLocationTypesTab => string.Equals(CurrentTab, LocationTypesTabKey, StringComparison.Ordinal);
    public bool IsModKeysTab => string.Equals(CurrentTab, ModKeysTabKey, StringComparison.Ordinal);
    public bool IsWarningsTab => string.Equals(CurrentTab, WarningsTabKey, StringComparison.Ordinal);
    public string OverviewTabText => IsOverviewTab ? "[Overview]" : "Overview";
    public string LocationTypesTabText => IsLocationTypesTab ? "[Location Types]" : "Location Types";
    public string ModKeysTabText => IsModKeysTab ? "[Mod Keys]" : "Mod Keys";
    public string WarningsTabText => IsWarningsTab ? "[Warnings]" : "Warnings";

    public string ScalarSummary => $"Enabled {EnabledText} | Large Egg {LargeEggText} | Magic {MagicText} | Price {PriceText}";
    public string EnabledText => Enabled ? "On" : "Off";
    public string LargeEggText => LargeEgg ? "On" : "Off";
    public string MagicText => IsMagic ? "On" : "Off";
    public string EnabledComparison => $"Base {(Draft.BaseEnabled ? "On" : "Off")} | Current {EnabledText}";
    public string LargeEggComparison => $"Base {(Draft.BaseLargeEgg ? "On" : "Off")} | Current {LargeEggText}";
    public string MagicComparison => $"Base {(Draft.BaseIsMagic ? "On" : "Off")} | Current {MagicText}";
    public string PriceComparison => $"Base {Draft.BasePrice} | Current {PriceText}";

    public bool IsDirty => Enabled != Draft.Enabled
        || LargeEgg != Draft.LargeEgg
        || IsMagic != Draft.IsMagic
        || PriceText != Draft.Price.ToString()
        || LocationTypes.Any(location => location.IsDirty)
        || ModGroups.Any(group => group.IsDirty)
        || HasNewCustomGroupText;

    public bool HasNewCustomGroupText => !string.IsNullOrWhiteSpace(NewModGroupId);
    public bool ScalarsDirty => Enabled != Draft.Enabled || LargeEgg != Draft.LargeEgg || IsMagic != Draft.IsMagic || PriceText != Draft.Price.ToString();
    public bool LocationTypesDirty => LocationTypes.Any(location => location.IsDirty);
    public bool ModGroupsDirty => ModGroups.Any(group => group.IsDirty) || HasNewCustomGroupText;
    public string ScalarSectionLabel => ScalarsDirty ? "Scalars *" : "Scalars";
    public string LocationTypesSectionLabel => LocationTypesDirty ? "Location Types *" : "Location Types";
    public string ModGroupsSectionLabel => ModGroupsDirty ? "Extra Mod Animal Keys *" : "Extra Mod Animal Keys";
    public string DirtySummary => IsDirty ? "Unsaved changes" : "Matches saved state";

    public bool TryBuildDraft(out WildAnimalDraft draft, out string error)
    {
        if (!int.TryParse(PriceText, out var parsedPrice))
        {
            draft = Draft;
            error = $"Price for {Key} must be a whole number.";
            return false;
        }

        draft = BuildDraft(parsedPrice);
        error = "";
        return true;
    }

    private void AddModGroupCore()
    {
        var modId = NewModGroupId.Trim();
        if (modId.Length == 0 || ModGroups.Any(group => string.Equals(group.ModId, modId, StringComparison.OrdinalIgnoreCase)))
            return;

        ModGroups.Add(new WildAnimalModGroupEditorViewModel(modId, Array.Empty<string>(), Array.Empty<string>(), RefreshState, RemoveModGroup));
        NewModGroupId = "";
        OnPropertyChanged(nameof(ModGroups));
        RefreshState();
    }

    private bool CanAddModGroup()
    {
        var modId = NewModGroupId.Trim();
        return modId.Length > 0 && ModGroups.All(group => !string.Equals(group.ModId, modId, StringComparison.OrdinalIgnoreCase));
    }

    private void SetTab(string tabKey)
    {
        if (!SetProperty(ref _currentTab, tabKey, propertyName: nameof(CurrentTab)))
            return;

        OnPropertyChanged(nameof(IsOverviewTab));
        OnPropertyChanged(nameof(IsLocationTypesTab));
        OnPropertyChanged(nameof(IsModKeysTab));
        OnPropertyChanged(nameof(IsWarningsTab));
        OnPropertyChanged(nameof(OverviewTabText));
        OnPropertyChanged(nameof(LocationTypesTabText));
        OnPropertyChanged(nameof(ModKeysTabText));
        OnPropertyChanged(nameof(WarningsTabText));
    }

    private void RemoveModGroup(WildAnimalModGroupEditorViewModel group)
    {
        if (!ModGroups.Remove(group))
            return;

        OnPropertyChanged(nameof(ModGroups));
        RefreshState();
    }

    private void RefreshState()
    {
        PriceError = int.TryParse(PriceText, out var parsedPrice) ? "" : $"Price for {Key} must be a whole number.";
        var draft = BuildDraft(int.TryParse(PriceText, out var validPrice) ? validPrice : Draft.Price);
        var visual = _service.DescribeDraft(draft);
        PreviewStatus = visual.Preview.StatusText;
        PreviewSprite = _service.TryLoadPreviewSprite(visual.Preview);
        WarningSummary = visual.WarningMessages.Count == 0 ? "No warnings." : string.Join(Environment.NewLine, visual.WarningMessages);
        HasWarnings = visual.HasWarnings || HasValidationErrors;
        HeaderStatus = HasValidationErrors
            ? PriceError
            : IsDirty
                ? "Unsaved changes"
                : "All changes saved";

        foreach (var group in ModGroups)
            group.RefreshVisualState(visual.ModGroups.FirstOrDefault(item => string.Equals(item.ModId, group.ModId, StringComparison.OrdinalIgnoreCase)));

        foreach (var location in LocationTypes)
            location.Refresh();

        OnPropertyChanged(nameof(HasValidationErrors));
        OnPropertyChanged(nameof(ScalarSummary));
        OnPropertyChanged(nameof(EnabledText));
        OnPropertyChanged(nameof(LargeEggText));
        OnPropertyChanged(nameof(MagicText));
        OnPropertyChanged(nameof(EnabledComparison));
        OnPropertyChanged(nameof(LargeEggComparison));
        OnPropertyChanged(nameof(MagicComparison));
        OnPropertyChanged(nameof(PriceComparison));
        OnPropertyChanged(nameof(HasPreviewSprite));
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(ScalarsDirty));
        OnPropertyChanged(nameof(LocationTypesDirty));
        OnPropertyChanged(nameof(ModGroupsDirty));
        OnPropertyChanged(nameof(ScalarSectionLabel));
        OnPropertyChanged(nameof(LocationTypesSectionLabel));
        OnPropertyChanged(nameof(ModGroupsSectionLabel));
        OnPropertyChanged(nameof(DirtySummary));

        AddModGroupCommand.NotifyCanExecuteChanged();
        ResetEnabledCommand.NotifyCanExecuteChanged();
        ResetLargeEggCommand.NotifyCanExecuteChanged();
        ResetIsMagicCommand.NotifyCanExecuteChanged();
        ResetPriceCommand.NotifyCanExecuteChanged();
    }

    private WildAnimalDraft BuildDraft(int parsedPrice)
    {
        return new WildAnimalDraft(
            Draft.Key,
            Draft.BaseEnabled,
            Enabled,
            Draft.BaseLargeEgg,
            LargeEgg,
            Draft.BasePrice,
            parsedPrice,
            Draft.BaseIsMagic,
            IsMagic,
            LocationTypes.Select(location => new WildAnimalDraftLocationType(location.Key, location.BaseAllowed, location.Allowed)).ToList(),
            ModGroups
                .Where(group => !string.IsNullOrWhiteSpace(group.ModId))
                .Select(group => new WildAnimalDraftModGroup(
                    group.ModId,
                    group.BaseKeys.ToList(),
                    WildAnimalsEditorHelpers.ParseCsvKeyList(group.KeysText).ToList()))
                .ToList());
    }
}

public sealed class WildAnimalLocationTypeEditorViewModel : ObservableObject
{
    private readonly Action _notifyChanged;
    private bool _allowed;

    public WildAnimalLocationTypeEditorViewModel(string key, bool baseAllowed, bool allowed, Action notifyChanged)
    {
        Key = key;
        BaseAllowed = baseAllowed;
        _allowed = allowed;
        _notifyChanged = notifyChanged;
    }

    public string Key { get; }
    public bool BaseAllowed { get; }
    public string Label => $"{Key} | Base {(BaseAllowed ? "On" : "Off")} | Effective {(Allowed ? "On" : "Off")}";
    public bool IsDirty => Allowed != BaseAllowed;

    public bool Allowed
    {
        get => _allowed;
        set
        {
            if (!SetProperty(ref _allowed, value))
                return;

            Refresh();
            _notifyChanged();
        }
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(IsDirty));
    }
}

public sealed class WildAnimalModGroupEditorViewModel : ObservableObject
{
    private readonly Action _notifyChanged;
    private readonly Action<WildAnimalModGroupEditorViewModel> _removeGroup;
    private string _keysText;
    private string _warningText = "No warnings.";

    public WildAnimalModGroupEditorViewModel(
        string modId,
        IReadOnlyList<string> baseKeys,
        IReadOnlyList<string> keys,
        Action notifyChanged,
        Action<WildAnimalModGroupEditorViewModel> removeGroup)
    {
        ModId = modId;
        BaseKeys = baseKeys.ToList();
        _keysText = string.Join(", ", keys);
        _notifyChanged = notifyChanged;
        _removeGroup = removeGroup;
        ResetCommand = new RelayCommand(Reset, () => IsDirty);
        ClearCommand = new RelayCommand(Clear, () => !string.IsNullOrWhiteSpace(KeysText));
        RemoveCommand = new RelayCommand(() => _removeGroup(this));
    }

    public string ModId { get; }
    public IReadOnlyList<string> BaseKeys { get; }
    public string BaseKeysText => BaseKeys.Count == 0 ? "(none)" : string.Join(", ", BaseKeys);
    public string BaseSummary => $"Base: {BaseKeysText}";
    public bool IsDirty => !WildAnimalsEditorHelpers.SequenceEqualIgnoreCase(BaseKeys, WildAnimalsEditorHelpers.ParseCsvKeyList(KeysText));
    public string EffectiveSummary => string.IsNullOrWhiteSpace(KeysText) ? "(none)" : string.Join(", ", WildAnimalsEditorHelpers.ParseCsvKeyList(KeysText));
    public string EffectiveLabel => $"Effective: {EffectiveSummary}";
    public string WarningText
    {
        get => _warningText;
        private set => SetProperty(ref _warningText, value);
    }

    public RelayCommand ResetCommand { get; }
    public RelayCommand ClearCommand { get; }
    public RelayCommand RemoveCommand { get; }

    public void ResetToBase() => ResetCommand.Execute(null);
    public void ClearOverride() => ClearCommand.Execute(null);
    public void RemoveGroup() => RemoveCommand.Execute(null);

    public string KeysText
    {
        get => _keysText;
        set
        {
            if (!SetProperty(ref _keysText, value))
                return;

            RefreshDerivedState();
            _notifyChanged();
        }
    }

    public void RefreshVisualState(WildAnimalDraftModGroupVisualState? state)
    {
        WarningText = state is null || state.UnknownKeys.Count == 0
            ? "No warnings."
            : $"Unknown farm animal keys: {string.Join(", ", state.UnknownKeys)}";
    }

    private void Reset()
    {
        KeysText = string.Join(", ", BaseKeys);
    }

    private void Clear()
    {
        KeysText = "";
    }

    private void RefreshDerivedState()
    {
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(BaseSummary));
        OnPropertyChanged(nameof(EffectiveSummary));
        OnPropertyChanged(nameof(EffectiveLabel));
        ResetCommand.NotifyCanExecuteChanged();
        ClearCommand.NotifyCanExecuteChanged();
    }
}
