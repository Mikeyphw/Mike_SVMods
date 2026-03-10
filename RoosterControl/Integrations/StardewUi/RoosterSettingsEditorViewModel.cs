using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using RoosterControl.Enums;
using RoosterControl.Framework.Config;

namespace RoosterControl.Integrations.StardewUi;

internal sealed class RoosterSettingsEditorViewModel : INotifyPropertyChanged
{
    private readonly RoosterControlConfig _config;
    private readonly Action _saveChanges;
    private readonly Func<string> _compatAvailabilityText;
    private readonly Func<string> _produceAvailabilityText;

    private RoosterSettingsDraft _draft;
    private RoosterSettingsDraft _snapshot;
    private RoosterType _selectedRoosterType;

    public event PropertyChangedEventHandler? PropertyChanged;

    public RoosterSettingsEditorViewModel(
        RoosterControlConfig config,
        Action saveChanges,
        Func<string> compatAvailabilityText,
        Func<string> produceAvailabilityText)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _saveChanges = saveChanges ?? throw new ArgumentNullException(nameof(saveChanges));
        _compatAvailabilityText = compatAvailabilityText ?? throw new ArgumentNullException(nameof(compatAvailabilityText));
        _produceAvailabilityText = produceAvailabilityText ?? throw new ArgumentNullException(nameof(produceAvailabilityText));

        _snapshot = RoosterSettingsDraft.FromConfig(_config);
        _draft = _snapshot.Clone();
        _selectedRoosterType = RoosterType.White;
        RebuildState();
    }

    public string Title => "Settings";
    public IReadOnlyList<string> UpdateRateOptions => Enum.GetNames(typeof(UpdateRateMode));
    public IReadOnlyList<string> HarvestTypeOptions => Enum.GetNames(typeof(HarvestType));
    public IReadOnlyList<string> RequiredBuildingOptions => Enum.GetNames(typeof(RequiredBuilding));
    public IReadOnlyList<string> RoosterTypeOptions => Enum.GetNames(typeof(RoosterType));

    public string SummaryText => $"{SelectedRoosterName} rooster settings | Update: {SelectedUpdateRate} | Harvest: {SelectedHarvestType}";
    public string CompatKeybindText => _draft.CompatKeybindText;
    public string ProduceKeybindText => _draft.ProduceKeybindText;
    public string CompatAvailabilityText => _compatAvailabilityText();
    public string ProduceAvailabilityText => _produceAvailabilityText();

    public string SelectedUpdateRate
    {
        get => _draft.UpdateRate.ToString();
        set
        {
            if (!Enum.TryParse<UpdateRateMode>(value, true, out var parsed) || _draft.UpdateRate == parsed)
                return;
            _draft.UpdateRate = parsed;
            MarkDirty(nameof(SelectedUpdateRate), nameof(SummaryText));
        }
    }

    public string SelectedHarvestType
    {
        get => _draft.HarvestType.ToString();
        set
        {
            if (!Enum.TryParse<HarvestType>(value, true, out var parsed) || _draft.HarvestType == parsed)
                return;
            _draft.HarvestType = parsed;
            MarkDirty(nameof(SelectedHarvestType), nameof(SummaryText));
        }
    }

    public bool EnableEggProbability
    {
        get => _draft.EnableEggProbability;
        set
        {
            if (_draft.EnableEggProbability == value)
                return;
            _draft.EnableEggProbability = value;
            MarkDirty(nameof(EnableEggProbability));
        }
    }

    public bool EnableLargeEggProbability
    {
        get => _draft.EnableLargeEggProbability;
        set
        {
            if (_draft.EnableLargeEggProbability == value)
                return;
            _draft.EnableLargeEggProbability = value;
            MarkDirty(nameof(EnableLargeEggProbability));
        }
    }

    public string EggProbabilityText
    {
        get => _draft.EggProbabilityText;
        set
        {
            if (_draft.EggProbabilityText == value)
                return;
            _draft.EggProbabilityText = value;
            MarkDirty(nameof(EggProbabilityText), nameof(ValidationText));
        }
    }

    public string LargeEggProbabilityText
    {
        get => _draft.LargeEggProbabilityText;
        set
        {
            if (_draft.LargeEggProbabilityText == value)
                return;
            _draft.LargeEggProbabilityText = value;
            MarkDirty(nameof(LargeEggProbabilityText), nameof(ValidationText));
        }
    }

    public string FertilizedEggProbabilityText
    {
        get => _draft.FertilizedEggProbabilityText;
        set
        {
            if (_draft.FertilizedEggProbabilityText == value)
                return;
            _draft.FertilizedEggProbabilityText = value;
            MarkDirty(nameof(FertilizedEggProbabilityText), nameof(ValidationText));
        }
    }

    public string RoosterToChickenRatioText
    {
        get => _draft.RoosterToChickenRatioText;
        set
        {
            if (_draft.RoosterToChickenRatioText == value)
                return;
            _draft.RoosterToChickenRatioText = value;
            MarkDirty(nameof(RoosterToChickenRatioText), nameof(ValidationText));
        }
    }

    public string BoostByRoosterText
    {
        get => _draft.BoostByRoosterText;
        set
        {
            if (_draft.BoostByRoosterText == value)
                return;
            _draft.BoostByRoosterText = value;
            MarkDirty(nameof(BoostByRoosterText), nameof(ValidationText));
        }
    }

    public string ChosenDailyLuckBonusText
    {
        get => _draft.ChosenDailyLuckBonusText;
        set
        {
            if (_draft.ChosenDailyLuckBonusText == value)
                return;
            _draft.ChosenDailyLuckBonusText = value;
            MarkDirty(nameof(ChosenDailyLuckBonusText), nameof(ValidationText));
        }
    }

    public string SelectedRoosterTypeName
    {
        get => _selectedRoosterType.ToString();
        set
        {
            if (!Enum.TryParse<RoosterType>(value, true, out var parsed) || _selectedRoosterType == parsed)
                return;
            _selectedRoosterType = parsed;
            RebuildRoosterState();
        }
    }

    public string SelectedRoosterName => _selectedRoosterType.ToString();

    private RoosterSettingsTypeDraft CurrentRooster => _draft.Roosters[_selectedRoosterType];

    public string SelectedRequiredBuilding
    {
        get => CurrentRooster.BuildingRequired.ToString();
        set
        {
            if (!Enum.TryParse<RequiredBuilding>(value, true, out var parsed) || CurrentRooster.BuildingRequired == parsed)
                return;
            CurrentRooster.BuildingRequired = parsed;
            MarkDirty(nameof(SelectedRequiredBuilding));
        }
    }

    public bool RoosterBuyable
    {
        get => CurrentRooster.Buyable;
        set
        {
            if (CurrentRooster.Buyable == value)
                return;
            CurrentRooster.Buyable = value;
            MarkDirty(nameof(RoosterBuyable));
        }
    }

    public bool EnableRoosterHatching
    {
        get => CurrentRooster.EnableRoosterHatching;
        set
        {
            if (CurrentRooster.EnableRoosterHatching == value)
                return;
            CurrentRooster.EnableRoosterHatching = value;
            MarkDirty(nameof(EnableRoosterHatching));
        }
    }

    public bool EditAllFemales
    {
        get => CurrentRooster.EditAllFemales;
        set
        {
            if (CurrentRooster.EditAllFemales == value)
                return;
            CurrentRooster.EditAllFemales = value;
            MarkDirty(nameof(EditAllFemales));
        }
    }

    public bool BypassBlueConditions
    {
        get => CurrentRooster.BypassBlueConditions;
        set
        {
            if (CurrentRooster.BypassBlueConditions == value)
                return;
            CurrentRooster.BypassBlueConditions = value;
            MarkDirty(nameof(BypassBlueConditions));
        }
    }

    public bool IsBlueRooster => _selectedRoosterType == RoosterType.Blue;

    public string PriceText
    {
        get => CurrentRooster.PriceText;
        set
        {
            if (CurrentRooster.PriceText == value)
                return;
            CurrentRooster.PriceText = value;
            MarkDirty(nameof(PriceText), nameof(ValidationText));
        }
    }

    public string MatureDaysText
    {
        get => CurrentRooster.MatureDaysText;
        set
        {
            if (CurrentRooster.MatureDaysText == value)
                return;
            CurrentRooster.MatureDaysText = value;
            MarkDirty(nameof(MatureDaysText), nameof(ValidationText));
        }
    }

    public string ProduceDaysText
    {
        get => CurrentRooster.ProduceDaysText;
        set
        {
            if (CurrentRooster.ProduceDaysText == value)
                return;
            CurrentRooster.ProduceDaysText = value;
            MarkDirty(nameof(ProduceDaysText), nameof(ValidationText));
        }
    }

    public string BlueEggChoiceText
    {
        get => CurrentRooster.BlueEggChoice;
        set
        {
            value ??= string.Empty;
            if (CurrentRooster.BlueEggChoice == value)
                return;
            CurrentRooster.BlueEggChoice = value;
            MarkDirty(nameof(BlueEggChoiceText));
        }
    }

    public string ValidationText => BuildValidationText();
    public bool CanSave => _draft.HasChangesComparedTo(_snapshot) && string.IsNullOrEmpty(ValidationText);
    public bool CanDiscard => _draft.HasChangesComparedTo(_snapshot);

    public void Save()
    {
        if (!CanSave || !TryParseDraft(out var parsed))
            return;

        parsed.ApplyTo(_config);
        _saveChanges();
        _snapshot = RoosterSettingsDraft.FromConfig(_config);
        _draft = _snapshot.Clone();
        RebuildState();
    }

    public void Discard()
    {
        _draft = _snapshot.Clone();
        RebuildState();
    }

    public void Reload()
    {
        _snapshot = RoosterSettingsDraft.FromConfig(_config);
        _draft = _snapshot.Clone();
        RebuildState();
    }

    private void RebuildState()
    {
        RebuildRoosterState();
        OnPropertyChanged(nameof(SelectedUpdateRate));
        OnPropertyChanged(nameof(SelectedHarvestType));
        OnPropertyChanged(nameof(EnableEggProbability));
        OnPropertyChanged(nameof(EnableLargeEggProbability));
        OnPropertyChanged(nameof(EggProbabilityText));
        OnPropertyChanged(nameof(LargeEggProbabilityText));
        OnPropertyChanged(nameof(FertilizedEggProbabilityText));
        OnPropertyChanged(nameof(RoosterToChickenRatioText));
        OnPropertyChanged(nameof(BoostByRoosterText));
        OnPropertyChanged(nameof(ChosenDailyLuckBonusText));
        OnPropertyChanged(nameof(CompatKeybindText));
        OnPropertyChanged(nameof(ProduceKeybindText));
        OnPropertyChanged(nameof(CompatAvailabilityText));
        OnPropertyChanged(nameof(ProduceAvailabilityText));
        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(ValidationText));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
    }

    private void RebuildRoosterState()
    {
        OnPropertyChanged(nameof(SelectedRoosterTypeName));
        OnPropertyChanged(nameof(SelectedRoosterName));
        OnPropertyChanged(nameof(SelectedRequiredBuilding));
        OnPropertyChanged(nameof(RoosterBuyable));
        OnPropertyChanged(nameof(EnableRoosterHatching));
        OnPropertyChanged(nameof(EditAllFemales));
        OnPropertyChanged(nameof(BypassBlueConditions));
        OnPropertyChanged(nameof(IsBlueRooster));
        OnPropertyChanged(nameof(PriceText));
        OnPropertyChanged(nameof(MatureDaysText));
        OnPropertyChanged(nameof(ProduceDaysText));
        OnPropertyChanged(nameof(BlueEggChoiceText));
        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(ValidationText));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
    }

    private void MarkDirty(params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
            OnPropertyChanged(propertyName);

        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
    }

    private string BuildValidationText()
    {
        var issues = new List<string>();
        if (!TryParseInt(_draft.EggProbabilityText, 0, 100, out _))
            issues.Add("Egg probability must be a whole number from 0 to 100.");
        if (!TryParseInt(_draft.LargeEggProbabilityText, 0, 100, out _))
            issues.Add("Large egg probability must be a whole number from 0 to 100.");
        if (!TryParseInt(_draft.FertilizedEggProbabilityText, 0, 100, out _))
            issues.Add("Fertilized egg probability must be a whole number from 0 to 100.");
        if (!TryParseInt(_draft.RoosterToChickenRatioText, 0, 100, out _))
            issues.Add("Rooster to chicken ratio must be a whole number from 0 to 100.");
        if (!TryParseFloat(_draft.BoostByRoosterText, 0f, 10f, out _))
            issues.Add("Boost by rooster must be a number from 0 to 10.");
        if (!TryParseInt(_draft.ChosenDailyLuckBonusText, 0, 100, out _))
            issues.Add("Chosen daily luck bonus must be a whole number from 0 to 100.");

        foreach (var rooster in _draft.Roosters.Values)
        {
            if (!TryParseInt(rooster.PriceText, 0, 50000, out _))
                issues.Add($"{rooster.Type} price must be a whole number from 0 to 50000.");
            if (!TryParseInt(rooster.MatureDaysText, 0, 60, out _))
                issues.Add($"{rooster.Type} mature days must be a whole number from 0 to 60.");
            if (!TryParseInt(rooster.ProduceDaysText, 1, 14, out _))
                issues.Add($"{rooster.Type} produce days must be a whole number from 1 to 14.");
        }

        return string.Join("\n", issues.Distinct(StringComparer.Ordinal));
    }

    private bool TryParseDraft(out RoosterSettingsDraft parsed)
    {
        parsed = _draft.Clone();
        if (!string.IsNullOrEmpty(ValidationText))
            return false;

        return true;
    }

    private static bool TryParseInt(string raw, int min, int max, out int value)
    {
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            return false;
        return value >= min && value <= max;
    }

    private static bool TryParseFloat(string raw, float min, float max, out float value)
    {
        if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return false;
        return value >= min && value <= max;
    }

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

internal sealed class RoosterSettingsDraft
{
    public UpdateRateMode UpdateRate { get; set; }
    public HarvestType HarvestType { get; set; }
    public bool EnableEggProbability { get; set; }
    public bool EnableLargeEggProbability { get; set; }
    public string EggProbabilityText { get; set; } = string.Empty;
    public string LargeEggProbabilityText { get; set; } = string.Empty;
    public string FertilizedEggProbabilityText { get; set; } = string.Empty;
    public string RoosterToChickenRatioText { get; set; } = string.Empty;
    public string BoostByRoosterText { get; set; } = string.Empty;
    public string ChosenDailyLuckBonusText { get; set; } = string.Empty;
    public string CompatKeybindText { get; set; } = string.Empty;
    public string ProduceKeybindText { get; set; } = string.Empty;
    public Dictionary<RoosterType, RoosterSettingsTypeDraft> Roosters { get; set; } = new();

    public static RoosterSettingsDraft FromConfig(RoosterControlConfig config)
    {
        var draft = new RoosterSettingsDraft
        {
            UpdateRate = config.General.UpdateRate,
            HarvestType = config.General.HarvestType,
            EnableEggProbability = config.General.EnableEggProbability,
            EnableLargeEggProbability = config.General.EnableLargeEggProbability,
            EggProbabilityText = config.General.EggProbability.ToString(CultureInfo.InvariantCulture),
            LargeEggProbabilityText = config.General.LargeEggProbability.ToString(CultureInfo.InvariantCulture),
            FertilizedEggProbabilityText = config.General.FertilizedEggProbability.ToString(CultureInfo.InvariantCulture),
            RoosterToChickenRatioText = config.General.RoosterToChickenRatio.ToString(CultureInfo.InvariantCulture),
            BoostByRoosterText = config.General.BoostByRooster.ToString(CultureInfo.InvariantCulture),
            ChosenDailyLuckBonusText = config.General.ChosenPercentageDailyLuckBonus.ToString(CultureInfo.InvariantCulture),
            CompatKeybindText = config.General.OpenCompatEditorKey.ToString(),
            ProduceKeybindText = config.General.OpenWhiteRoosterProduceEditorKey.ToString(),
        };

        foreach (var (type, rooster) in config.Roosters)
            draft.Roosters[type] = RoosterSettingsTypeDraft.FromConfig(type, rooster);

        return draft;
    }

    public RoosterSettingsDraft Clone()
        => new()
        {
            UpdateRate = UpdateRate,
            HarvestType = HarvestType,
            EnableEggProbability = EnableEggProbability,
            EnableLargeEggProbability = EnableLargeEggProbability,
            EggProbabilityText = EggProbabilityText,
            LargeEggProbabilityText = LargeEggProbabilityText,
            FertilizedEggProbabilityText = FertilizedEggProbabilityText,
            RoosterToChickenRatioText = RoosterToChickenRatioText,
            BoostByRoosterText = BoostByRoosterText,
            ChosenDailyLuckBonusText = ChosenDailyLuckBonusText,
            CompatKeybindText = CompatKeybindText,
            ProduceKeybindText = ProduceKeybindText,
            Roosters = Roosters.ToDictionary(pair => pair.Key, pair => pair.Value.Clone()),
        };

    public bool HasChangesComparedTo(RoosterSettingsDraft other)
    {
        if (other is null)
            return true;

        if (UpdateRate != other.UpdateRate
            || HarvestType != other.HarvestType
            || EnableEggProbability != other.EnableEggProbability
            || EnableLargeEggProbability != other.EnableLargeEggProbability
            || !string.Equals(EggProbabilityText, other.EggProbabilityText, StringComparison.Ordinal)
            || !string.Equals(LargeEggProbabilityText, other.LargeEggProbabilityText, StringComparison.Ordinal)
            || !string.Equals(FertilizedEggProbabilityText, other.FertilizedEggProbabilityText, StringComparison.Ordinal)
            || !string.Equals(RoosterToChickenRatioText, other.RoosterToChickenRatioText, StringComparison.Ordinal)
            || !string.Equals(BoostByRoosterText, other.BoostByRoosterText, StringComparison.Ordinal)
            || !string.Equals(ChosenDailyLuckBonusText, other.ChosenDailyLuckBonusText, StringComparison.Ordinal))
            return true;

        foreach (var (type, rooster) in Roosters)
        {
            if (!other.Roosters.TryGetValue(type, out var otherRooster) || rooster.HasChangesComparedTo(otherRooster))
                return true;
        }

        return false;
    }

    public void ApplyTo(RoosterControlConfig config)
    {
        config.General.UpdateRate = UpdateRate;
        config.General.HarvestType = HarvestType;
        config.General.EnableEggProbability = EnableEggProbability;
        config.General.EnableLargeEggProbability = EnableLargeEggProbability;
        config.General.EggProbability = int.Parse(EggProbabilityText, CultureInfo.InvariantCulture);
        config.General.LargeEggProbability = int.Parse(LargeEggProbabilityText, CultureInfo.InvariantCulture);
        config.General.FertilizedEggProbability = int.Parse(FertilizedEggProbabilityText, CultureInfo.InvariantCulture);
        config.General.RoosterToChickenRatio = int.Parse(RoosterToChickenRatioText, CultureInfo.InvariantCulture);
        config.General.BoostByRooster = float.Parse(BoostByRoosterText, CultureInfo.InvariantCulture);
        config.General.ChosenPercentageDailyLuckBonus = int.Parse(ChosenDailyLuckBonusText, CultureInfo.InvariantCulture);

        foreach (var (type, roosterDraft) in Roosters)
        {
            if (!config.Roosters.TryGetValue(type, out var rooster))
            {
                rooster = new RoosterSettings();
                config.Roosters[type] = rooster;
            }

            roosterDraft.ApplyTo(rooster);
        }
    }
}

internal sealed class RoosterSettingsTypeDraft
{
    public RoosterType Type { get; init; }
    public RequiredBuilding BuildingRequired { get; set; }
    public bool Buyable { get; set; }
    public bool EnableRoosterHatching { get; set; }
    public bool EditAllFemales { get; set; }
    public bool BypassBlueConditions { get; set; }
    public string PriceText { get; set; } = string.Empty;
    public string MatureDaysText { get; set; } = string.Empty;
    public string ProduceDaysText { get; set; } = string.Empty;
    public string BlueEggChoice { get; set; } = string.Empty;

    public static RoosterSettingsTypeDraft FromConfig(RoosterType type, RoosterSettings rooster)
        => new()
        {
            Type = type,
            BuildingRequired = rooster.BuildingRequired,
            Buyable = rooster.Buyable,
            EnableRoosterHatching = rooster.EnableRoosterHatching,
            EditAllFemales = rooster.EditAllFemales,
            BypassBlueConditions = rooster.BypassBlueConditions,
            PriceText = rooster.Price.ToString(CultureInfo.InvariantCulture),
            MatureDaysText = rooster.MatureDays.ToString(CultureInfo.InvariantCulture),
            ProduceDaysText = rooster.ProduceDays.ToString(CultureInfo.InvariantCulture),
            BlueEggChoice = rooster.BlueEggChoice ?? string.Empty,
        };

    public RoosterSettingsTypeDraft Clone()
        => new()
        {
            Type = Type,
            BuildingRequired = BuildingRequired,
            Buyable = Buyable,
            EnableRoosterHatching = EnableRoosterHatching,
            EditAllFemales = EditAllFemales,
            BypassBlueConditions = BypassBlueConditions,
            PriceText = PriceText,
            MatureDaysText = MatureDaysText,
            ProduceDaysText = ProduceDaysText,
            BlueEggChoice = BlueEggChoice,
        };

    public bool HasChangesComparedTo(RoosterSettingsTypeDraft other)
        => other is null
            || BuildingRequired != other.BuildingRequired
            || Buyable != other.Buyable
            || EnableRoosterHatching != other.EnableRoosterHatching
            || EditAllFemales != other.EditAllFemales
            || BypassBlueConditions != other.BypassBlueConditions
            || !string.Equals(PriceText, other.PriceText, StringComparison.Ordinal)
            || !string.Equals(MatureDaysText, other.MatureDaysText, StringComparison.Ordinal)
            || !string.Equals(ProduceDaysText, other.ProduceDaysText, StringComparison.Ordinal)
            || !string.Equals(BlueEggChoice, other.BlueEggChoice, StringComparison.Ordinal);

    public void ApplyTo(RoosterSettings rooster)
    {
        rooster.BuildingRequired = BuildingRequired;
        rooster.Buyable = Buyable;
        rooster.EnableRoosterHatching = EnableRoosterHatching;
        rooster.EditAllFemales = EditAllFemales;
        rooster.BypassBlueConditions = BypassBlueConditions;
        rooster.Price = int.Parse(PriceText, CultureInfo.InvariantCulture);
        rooster.MatureDays = int.Parse(MatureDaysText, CultureInfo.InvariantCulture);
        rooster.ProduceDays = int.Parse(ProduceDaysText, CultureInfo.InvariantCulture);
        rooster.BlueEggChoice = BlueEggChoice.Trim();
    }
}
