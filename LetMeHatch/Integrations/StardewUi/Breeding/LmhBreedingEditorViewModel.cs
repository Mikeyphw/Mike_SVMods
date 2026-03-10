using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static LetMeHatch.EnumsAndStrings.Enums;

namespace LetMeHatch.Integrations.StardewUi.Breeding;

public sealed class LmhBreedingEditorViewModel : INotifyPropertyChanged
{
    private readonly LmhBreedingEditorService _service;
    private readonly Func<IReadOnlyList<LmhBreedingPackRecord>> _loadPacks;
    private readonly Func<string, string, LmhBreedingFemaleRecord?> _loadFemale;
    private readonly Action<string, string, LmhBreedingDraft> _saveDraft;
    private readonly Func<string, Tuple<Texture2D, Rectangle>?>? _loadPreviewSprite;

    private List<LmhBreedingPackRecord> _packRecords = new();
    private LmhBreedingFemaleRecord? _selectedFemaleRecord;
    private LmhBreedingDraft? _draft;
    private string? _selectedPackId;
    private string? _selectedFemaleKey;
    private string? _selectedMaleKey;
    private bool _isOverviewTab = true;

    private string _normalEggChanceText = string.Empty;
    private string _largeEggChanceText = string.Empty;
    private string _fertilizedEggChanceText = string.Empty;
    private string _selectedMaleChanceText = string.Empty;
    private string _selectedMaleBoostText = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LmhBreedingEditorViewModel(
        LmhBreedingEditorService service,
        Func<IReadOnlyList<LmhBreedingPackRecord>> loadPacks,
        Func<string, string, LmhBreedingFemaleRecord?> loadFemale,
        Action<string, string, LmhBreedingDraft> saveDraft,
        Func<string, Tuple<Texture2D, Rectangle>?>? loadPreviewSprite = null)
    {
        _service = service;
        _loadPacks = loadPacks;
        _loadFemale = loadFemale;
        _saveDraft = saveDraft;
        _loadPreviewSprite = loadPreviewSprite;
        Reload();
    }

    public string Title => "Breeding Editor";
    public string SelectedPack => SelectedPackRecord?.DisplayName ?? string.Empty;
    public IReadOnlyList<string> PackOptions => _packRecords.Select(x => x.ContentPackId).ToList();
    public string SelectedPackOption
    {
        get => _selectedPackId ?? string.Empty;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(_selectedPackId, value, StringComparison.OrdinalIgnoreCase))
                return;

            _selectedPackId = value;
            _selectedFemaleKey = null;
            _selectedMaleKey = null;
            _isOverviewTab = true;
            LoadSelectedFemale();
            RebuildLists();
        }
    }
    public string SelectedFemale => SelectedFemaleRecord?.FemaleKey ?? string.Empty;
    public bool HasSelectedFemale => SelectedFemaleRecord is not null;
    public bool IsOverviewTab => _isOverviewTab;
    public bool IsRulesTab => !_isOverviewTab;
    public bool CanSave => HasSelectedFemale && !HasValidationErrors && HasChanges;
    public bool CanDiscard => HasSelectedFemale && HasChanges;

    public string SummaryText
    {
        get
        {
            if (SelectedFemaleRecord is null)
                return $"Packs: {Packs.Count}";

            var maleCount = SelectedFemaleRecord.Males.Count;
            return $"{SelectedFemaleRecord.FemaleKey} | Males: {maleCount} | Pack: {SelectedPack}";
        }
    }
    public string SelectedFemaleSummaryText
        => SelectedFemaleRecord is null
            ? "Select a female to edit breeding rules."
            : $"Pack: {SelectedPack} | Female: {SelectedFemaleRecord.FemaleKey} | Adjustable eggs: {(SelectedFemaleRecord.AdjustableEggChances ? "Yes" : "No")}";
    public string EggChanceSummaryText
        => Draft is null
            ? string.Empty
            : $"Normal: {NormalEggChanceText} | Large: {LargeEggChanceText} | Fertilized: {FertilizedEggChanceText}";
    public string SelectedMaleSummaryText
    {
        get
        {
            if (SelectedMaleRecord is null)
                return "Select a male to edit male-specific rules.";

            return $"Male: {SelectedMaleRecord.DisplayName} | Enabled: {GetDraftEnabled(SelectedMaleRecord.MaleKey)} | Chance: {SelectedMaleChanceText} | Boost: {SelectedMaleBoostText}";
        }
    }
    public bool HasValidationErrors => !TryParseDraftValues(out _, out _, out _, out _, out _);
    public string ValidationText => HasValidationErrors
        ? "One or more chance or boost values are invalid."
        : "All numeric values are valid.";

    public IReadOnlyList<LmhBreedingPackItemViewModel> Packs { get; private set; } = Array.Empty<LmhBreedingPackItemViewModel>();
    public IReadOnlyList<LmhBreedingFemaleItemViewModel> Females { get; private set; } = Array.Empty<LmhBreedingFemaleItemViewModel>();
    public IReadOnlyList<LmhBreedingMaleItemViewModel> Males { get; private set; } = Array.Empty<LmhBreedingMaleItemViewModel>();

    public string ApplyDailyLuckBoostLabel => Draft?.ApplyDailyLuckBoost == true ? "Daily boost: On" : "Daily boost: Off";
    public string NormalEggChanceText { get => _normalEggChanceText; set { if (_normalEggChanceText != value) { _normalEggChanceText = value; OnPropertyChanged(nameof(NormalEggChanceText)); OnPropertyChanged(nameof(CanSave)); } } }
    public string LargeEggChanceText { get => _largeEggChanceText; set { if (_largeEggChanceText != value) { _largeEggChanceText = value; OnPropertyChanged(nameof(LargeEggChanceText)); OnPropertyChanged(nameof(CanSave)); } } }
    public string FertilizedEggChanceText { get => _fertilizedEggChanceText; set { if (_fertilizedEggChanceText != value) { _fertilizedEggChanceText = value; OnPropertyChanged(nameof(FertilizedEggChanceText)); OnPropertyChanged(nameof(CanSave)); } } }
    public string SelectedMale => SelectedMaleRecord?.DisplayName ?? string.Empty;
    public bool HasSelectedMale => SelectedMaleRecord is not null;
    public string SelectedMaleEnabledLabel => SelectedMaleEnabled ? "Male enabled: On" : "Male enabled: Off";
    public string SelectedMaleAlternatePurchaseLabel => Draft?.AddAsAlternatePurchase == true ? "Alternate purchase: On" : "Alternate purchase: Off";
    public string SelectedMaleChanceText { get => _selectedMaleChanceText; set { if (_selectedMaleChanceText != value) { _selectedMaleChanceText = value; OnPropertyChanged(nameof(SelectedMaleChanceText)); OnPropertyChanged(nameof(CanSave)); } } }
    public string SelectedMaleBoostText { get => _selectedMaleBoostText; set { if (_selectedMaleBoostText != value) { _selectedMaleBoostText = value; OnPropertyChanged(nameof(SelectedMaleBoostText)); OnPropertyChanged(nameof(CanSave)); } } }
    public string SelectedMaleEggsText => SelectedMaleRecord is null
        ? string.Empty
        : string.Join(" | ", SelectedMaleRecord.EggIds.OrderBy(x => x.Key).Select(BuildEggSummary));

    private LmhBreedingPackRecord? SelectedPackRecord => _packRecords.FirstOrDefault(x => x.ContentPackId == _selectedPackId);
    private LmhBreedingFemaleRecord? SelectedFemaleRecord => _selectedFemaleRecord;
    private LmhBreedingMaleRecord? SelectedMaleRecord => SelectedFemaleRecord?.Males.FirstOrDefault(x => x.MaleKey == _selectedMaleKey);
    private LmhBreedingDraft? Draft => _draft;
    private bool SelectedMaleEnabled => SelectedMaleRecord is not null && Draft is not null && Draft.Enabled.TryGetValue(SelectedMaleRecord.MaleKey, out var enabled) ? enabled : SelectedMaleRecord?.Enabled ?? false;

    private string BuildEggSummary(KeyValuePair<ItemType, string?> pair)
    {
        if (SelectedMaleRecord is null)
            return $"{pair.Key}: {pair.Value ?? "-"}";

        if (!SelectedMaleRecord.EggCandidatePools.TryGetValue(pair.Key, out var pool) || pool.Count <= 1)
            return $"{pair.Key}: {pair.Value ?? "-"}";

        var values = string.Join(", ", pool);
        return $"{pair.Key}: random from [{values}]";
    }

    private bool HasChanges
    {
        get
        {
            if (SelectedFemaleRecord is null || Draft is null)
                return false;

            if (Draft.ApplyDailyLuckBoost != SelectedFemaleRecord.ApplyDailyLuckBoost)
                return true;

            foreach (var eggType in SelectedFemaleRecord.EggChances.Keys)
            {
                if (!Draft.EggChances.TryGetValue(eggType, out var current) || current != SelectedFemaleRecord.EggChances[eggType])
                    return true;
            }

            foreach (var male in SelectedFemaleRecord.Males)
            {
                if (Draft.Enabled.TryGetValue(male.MaleKey, out var enabled) && enabled != male.Enabled)
                    return true;
                if (Draft.MaleChance.TryGetValue(male.MaleKey, out var chance) && chance != male.MaleChance)
                    return true;
                if (Draft.BoostByMale.TryGetValue(male.MaleKey, out var boost) && boost != male.BoostByMale)
                    return true;
            }

            return Draft.AddAsAlternatePurchase != SelectedFemaleRecord.Males.Any(x => x.IsCloned && x.AddAsAlternatePurchase);
        }
    }

    public void OpenPack(LmhBreedingPackItemViewModel pack)
    {
        _selectedPackId = pack.ContentPackId;
        var firstFemale = SelectedPackRecord?.FemaleKeys.FirstOrDefault();
        _selectedFemaleKey = firstFemale;
        LoadSelectedFemale();
        RebuildLists();
    }

    public void OpenFemale(LmhBreedingFemaleItemViewModel female)
    {
        _selectedFemaleKey = female.FemaleKey;
        LoadSelectedFemale();
        RebuildLists();
    }

    public void OpenMale(LmhBreedingMaleItemViewModel male)
    {
        _selectedMaleKey = male.MaleKey;
        SyncSelectedMaleText();
        NotifyDetailState();
    }

    public void ShowOverviewTab()
    {
        _isOverviewTab = true;
        NotifyTabState();
    }

    public void ShowRulesTab()
    {
        _isOverviewTab = false;
        NotifyTabState();
    }

    public void ToggleApplyDailyLuckBoost()
    {
        if (Draft is null)
            return;

        _draft = Draft with { ApplyDailyLuckBoost = !Draft.ApplyDailyLuckBoost };
        OnPropertyChanged(nameof(ApplyDailyLuckBoostLabel));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
    }

    public void ToggleSelectedMaleEnabled()
    {
        if (Draft is null || SelectedMaleRecord is null)
            return;

        var enabled = new Dictionary<string, bool>(Draft.Enabled, StringComparer.OrdinalIgnoreCase)
        {
            [SelectedMaleRecord.MaleKey] = !SelectedMaleEnabled
        };
        _draft = Draft with { Enabled = enabled };
        RebuildLists();
    }

    public void ToggleSelectedMaleAlternatePurchase()
    {
        if (Draft is null || SelectedMaleRecord is null || !SelectedMaleRecord.IsCloned)
            return;

        _draft = Draft with { AddAsAlternatePurchase = !Draft.AddAsAlternatePurchase };
        NotifyDetailState();
    }

    public void Save()
    {
        if (SelectedFemaleRecord is null || Draft is null)
            return;
        if (!TryParseDraftValues(out var normal, out var large, out var fertilized, out var maleChance, out var maleBoost))
            return;

        var eggChances = new Dictionary<ItemType, float>(Draft.EggChances)
        {
            [ItemType.Normal] = normal,
            [ItemType.Large] = large,
            [ItemType.FertilizedEgg] = fertilized
        };
        var maleChances = new Dictionary<string, float>(Draft.MaleChance, StringComparer.OrdinalIgnoreCase);
        var boostByMale = new Dictionary<string, float>(Draft.BoostByMale, StringComparer.OrdinalIgnoreCase);
        if (SelectedMaleRecord is not null)
        {
            maleChances[SelectedMaleRecord.MaleKey] = maleChance;
            boostByMale[SelectedMaleRecord.MaleKey] = maleBoost;
        }

        _draft = Draft with { EggChances = eggChances, MaleChance = maleChances, BoostByMale = boostByMale };
        _saveDraft(SelectedFemaleRecord.ContentPackId, SelectedFemaleRecord.FemaleKey, _draft);
        Reload();
    }

    public void Discard()
    {
        if (SelectedFemaleRecord is null)
            return;
        LoadSelectedFemale();
        RebuildLists();
    }

    public void Reload()
    {
        _packRecords = _loadPacks().ToList();
        _selectedPackId ??= _packRecords.FirstOrDefault()?.ContentPackId;
        LoadSelectedFemale();
        RebuildLists();
    }

    private void LoadSelectedFemale()
    {
        _selectedFemaleRecord = null;
        _draft = null;
        _selectedMaleKey = null;

        if (SelectedPackRecord is null || string.IsNullOrWhiteSpace(_selectedFemaleKey))
        {
            NotifyAll();
            return;
        }

        _selectedFemaleRecord = _loadFemale(SelectedPackRecord.ContentPackId, _selectedFemaleKey!);
        if (_selectedFemaleRecord is null)
        {
            NotifyAll();
            return;
        }

        _draft = new LmhBreedingDraft(
            _selectedFemaleRecord.ApplyDailyLuckBoost,
            new Dictionary<ItemType, float>(_selectedFemaleRecord.EggChances),
            _selectedFemaleRecord.Males.ToDictionary(x => x.MaleKey, x => x.Enabled, StringComparer.OrdinalIgnoreCase),
            _selectedFemaleRecord.Males.ToDictionary(x => x.MaleKey, x => x.MaleChance, StringComparer.OrdinalIgnoreCase),
            _selectedFemaleRecord.Males.ToDictionary(x => x.MaleKey, x => x.BoostByMale, StringComparer.OrdinalIgnoreCase),
            _selectedFemaleRecord.Males.Any(x => x.IsCloned && x.AddAsAlternatePurchase));

        _selectedMaleKey = _selectedFemaleRecord.Males.FirstOrDefault()?.MaleKey;
        SyncDraftText();
        SyncSelectedMaleText();
        NotifyAll();
    }

    private void RebuildLists()
    {
        Packs = _packRecords
            .Select(record => new LmhBreedingPackItemViewModel(record.ContentPackId, record.DisplayName, $"{record.FemaleKeys.Count} females", OpenPack))
            .ToList();

        Females = SelectedPackRecord?.FemaleKeys
            .Select(key => new LmhBreedingFemaleItemViewModel(
                key,
                key,
                "Open female rules",
                _loadPreviewSprite?.Invoke(key),
                OpenFemale))
            .ToList()
            ?? new List<LmhBreedingFemaleItemViewModel>();

        Males = SelectedFemaleRecord?.Males
            .Select(record => new LmhBreedingMaleItemViewModel(record.MaleKey, record.DisplayName, $"{GetDraftEnabled(record.MaleKey)} | chance {GetDraftMaleChance(record.MaleKey).ToString("0.##", CultureInfo.InvariantCulture)}", OpenMale))
            .ToList()
            ?? new List<LmhBreedingMaleItemViewModel>();

        NotifyAll();
    }

    private void SyncDraftText()
    {
        if (Draft is null)
            return;

        NormalEggChanceText = FormatFloat(Draft.EggChances[ItemType.Normal]);
        LargeEggChanceText = FormatFloat(Draft.EggChances[ItemType.Large]);
        FertilizedEggChanceText = FormatFloat(Draft.EggChances[ItemType.FertilizedEgg]);
    }

    private void SyncSelectedMaleText()
    {
        if (SelectedMaleRecord is null || Draft is null)
        {
            SelectedMaleChanceText = string.Empty;
            SelectedMaleBoostText = string.Empty;
            return;
        }

        SelectedMaleChanceText = FormatFloat(GetDraftMaleChance(SelectedMaleRecord.MaleKey));
        SelectedMaleBoostText = FormatFloat(GetDraftBoostByMale(SelectedMaleRecord.MaleKey));
    }

    private bool TryParseDraftValues(out float normal, out float large, out float fertilized, out float maleChance, out float maleBoost)
    {
        var parsedNormal = float.TryParse(NormalEggChanceText, NumberStyles.Float, CultureInfo.InvariantCulture, out normal);
        var parsedLarge = float.TryParse(LargeEggChanceText, NumberStyles.Float, CultureInfo.InvariantCulture, out large);
        var parsedFertilized = float.TryParse(FertilizedEggChanceText, NumberStyles.Float, CultureInfo.InvariantCulture, out fertilized);
        var ok = parsedNormal && parsedLarge && parsedFertilized;

        maleChance = 0.5f;
        maleBoost = 0.5f;
        if (SelectedMaleRecord is not null)
        {
            ok = ok &&
                float.TryParse(SelectedMaleChanceText, NumberStyles.Float, CultureInfo.InvariantCulture, out maleChance) &&
                float.TryParse(SelectedMaleBoostText, NumberStyles.Float, CultureInfo.InvariantCulture, out maleBoost);
        }

        return ok;
    }

    private string GetDraftEnabled(string maleKey) => Draft is not null && Draft.Enabled.TryGetValue(maleKey, out var enabled) && enabled ? "enabled" : "disabled";
    private float GetDraftMaleChance(string maleKey) => Draft is not null && Draft.MaleChance.TryGetValue(maleKey, out var value) ? value : 0.5f;
    private float GetDraftBoostByMale(string maleKey) => Draft is not null && Draft.BoostByMale.TryGetValue(maleKey, out var value) ? value : 0.5f;
    private static string FormatFloat(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private void NotifyTabState()
    {
        OnPropertyChanged(nameof(IsOverviewTab));
        OnPropertyChanged(nameof(IsRulesTab));
    }

    private void NotifyDetailState()
    {
        OnPropertyChanged(nameof(SelectedFemale));
        OnPropertyChanged(nameof(HasSelectedFemale));
        OnPropertyChanged(nameof(SelectedPack));
        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(SelectedFemaleSummaryText));
        OnPropertyChanged(nameof(EggChanceSummaryText));
        OnPropertyChanged(nameof(ApplyDailyLuckBoostLabel));
        OnPropertyChanged(nameof(SelectedMale));
        OnPropertyChanged(nameof(HasSelectedMale));
        OnPropertyChanged(nameof(SelectedMaleSummaryText));
        OnPropertyChanged(nameof(SelectedMaleEnabledLabel));
        OnPropertyChanged(nameof(SelectedMaleAlternatePurchaseLabel));
        OnPropertyChanged(nameof(SelectedMaleEggsText));
        OnPropertyChanged(nameof(HasValidationErrors));
        OnPropertyChanged(nameof(ValidationText));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
    }

    private void NotifyAll()
    {
        OnPropertyChanged(nameof(Packs));
        OnPropertyChanged(nameof(PackOptions));
        OnPropertyChanged(nameof(SelectedPackOption));
        OnPropertyChanged(nameof(Females));
        OnPropertyChanged(nameof(Males));
        NotifyDetailState();
        NotifyTabState();
    }

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class LmhBreedingPackItemViewModel
{
    private readonly Action<LmhBreedingPackItemViewModel> _open;
    public LmhBreedingPackItemViewModel(string contentPackId, string buttonText, string metaText, Action<LmhBreedingPackItemViewModel> open)
    {
        ContentPackId = contentPackId;
        ButtonText = buttonText;
        MetaText = metaText;
        _open = open;
    }
    public string ContentPackId { get; }
    public string ButtonText { get; }
    public string MetaText { get; }
    public void OpenPack() => _open(this);
}

public sealed class LmhBreedingFemaleItemViewModel
{
    private readonly Action<LmhBreedingFemaleItemViewModel> _open;
    public LmhBreedingFemaleItemViewModel(
        string femaleKey,
        string buttonText,
        string metaText,
        Tuple<Texture2D, Rectangle>? previewSprite,
        Action<LmhBreedingFemaleItemViewModel> open)
    {
        FemaleKey = femaleKey;
        ButtonText = buttonText;
        MetaText = metaText;
        PreviewSprite = previewSprite;
        _open = open;
    }
    public string FemaleKey { get; }
    public string ButtonText { get; }
    public string MetaText { get; }
    public Tuple<Texture2D, Rectangle>? PreviewSprite { get; }
    public bool HasPreviewSprite => PreviewSprite is not null;
    public bool MissingPreviewSprite => !HasPreviewSprite;
    public string TooltipText => $"{ButtonText}\n{MetaText}";
    public void OpenFemale() => _open(this);
}

public sealed class LmhBreedingMaleItemViewModel
{
    private readonly Action<LmhBreedingMaleItemViewModel> _open;
    public LmhBreedingMaleItemViewModel(string maleKey, string buttonText, string metaText, Action<LmhBreedingMaleItemViewModel> open)
    {
        MaleKey = maleKey;
        ButtonText = buttonText;
        MetaText = metaText;
        _open = open;
    }
    public string MaleKey { get; }
    public string ButtonText { get; }
    public string MetaText { get; }
    public void OpenMale() => _open(this);
}
