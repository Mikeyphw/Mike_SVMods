using System;
using System.ComponentModel;
using System.Globalization;
using RoosterControl.Framework.Config;

namespace RoosterControl.Integrations.StardewUi;

internal sealed class RoosterDebugEditorViewModel : INotifyPropertyChanged
{
    private readonly RoosterControlConfig _config;
    private readonly Action _saveChanges;
    private RoosterDebugDraft _draft;
    private RoosterDebugDraft _snapshot;

    public event PropertyChangedEventHandler? PropertyChanged;

    public RoosterDebugEditorViewModel(RoosterControlConfig config, Action saveChanges)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _saveChanges = saveChanges ?? throw new ArgumentNullException(nameof(saveChanges));
        _snapshot = RoosterDebugDraft.FromConfig(_config);
        _draft = _snapshot.Clone();
    }

    public bool EnableDebug
    {
        get => _draft.EnableDebug;
        set
        {
            if (_draft.EnableDebug == value)
                return;
            _draft.EnableDebug = value;
            MarkDirty(nameof(EnableDebug));
        }
    }

    public bool VerboseLogging
    {
        get => _draft.VerboseLogging;
        set
        {
            if (_draft.VerboseLogging == value)
                return;
            _draft.VerboseLogging = value;
            MarkDirty(nameof(VerboseLogging));
        }
    }

    public bool TestingLogging
    {
        get => _draft.TestingLogging;
        set
        {
            if (_draft.TestingLogging == value)
                return;
            _draft.TestingLogging = value;
            MarkDirty(nameof(TestingLogging));
        }
    }

    public string BonusCalculationMultiplierText
    {
        get => _draft.BonusCalculationMultiplierText;
        set
        {
            if (_draft.BonusCalculationMultiplierText == value)
                return;
            _draft.BonusCalculationMultiplierText = value;
            MarkDirty(nameof(BonusCalculationMultiplierText), nameof(ValidationText));
        }
    }

    public string SuperUnluckyBonusMultiplierText
    {
        get => _draft.SuperUnluckyBonusMultiplierText;
        set
        {
            if (_draft.SuperUnluckyBonusMultiplierText == value)
                return;
            _draft.SuperUnluckyBonusMultiplierText = value;
            MarkDirty(nameof(SuperUnluckyBonusMultiplierText), nameof(ValidationText));
        }
    }

    public string SuperLuckyBonusMultiplierText
    {
        get => _draft.SuperLuckyBonusMultiplierText;
        set
        {
            if (_draft.SuperLuckyBonusMultiplierText == value)
                return;
            _draft.SuperLuckyBonusMultiplierText = value;
            MarkDirty(nameof(SuperLuckyBonusMultiplierText), nameof(ValidationText));
        }
    }

    public string LuckAdjustmentOffsetText
    {
        get => _draft.LuckAdjustmentOffsetText;
        set
        {
            if (_draft.LuckAdjustmentOffsetText == value)
                return;
            _draft.LuckAdjustmentOffsetText = value;
            MarkDirty(nameof(LuckAdjustmentOffsetText), nameof(ValidationText));
        }
    }

    public string ValidationText
    {
        get
        {
#if RCTRL_DEV
            if (!float.TryParse(_draft.BonusCalculationMultiplierText, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                return "Bonus calculation multiplier must be numeric.";
            if (!float.TryParse(_draft.SuperUnluckyBonusMultiplierText, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                return "Super unlucky multiplier must be numeric.";
            if (!float.TryParse(_draft.SuperLuckyBonusMultiplierText, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                return "Super lucky multiplier must be numeric.";
            if (!float.TryParse(_draft.LuckAdjustmentOffsetText, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                return "Luck adjustment offset must be numeric.";
#endif
            return string.Empty;
        }
    }

    public bool CanSave => _draft.HasChangesComparedTo(_snapshot) && string.IsNullOrEmpty(ValidationText);
    public bool CanDiscard => _draft.HasChangesComparedTo(_snapshot);

    public void Save()
    {
        if (!CanSave)
            return;

        _draft.ApplyTo(_config);
        _saveChanges();
        _snapshot = RoosterDebugDraft.FromConfig(_config);
        _draft = _snapshot.Clone();
        NotifyAll();
    }

    public void Discard()
    {
        _draft = _snapshot.Clone();
        NotifyAll();
    }

    public void Reload()
    {
        _snapshot = RoosterDebugDraft.FromConfig(_config);
        _draft = _snapshot.Clone();
        NotifyAll();
    }

    private void MarkDirty(params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
            OnPropertyChanged(propertyName);

        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
    }

    private void NotifyAll()
    {
        OnPropertyChanged(nameof(EnableDebug));
        OnPropertyChanged(nameof(VerboseLogging));
        OnPropertyChanged(nameof(TestingLogging));
        OnPropertyChanged(nameof(BonusCalculationMultiplierText));
        OnPropertyChanged(nameof(SuperUnluckyBonusMultiplierText));
        OnPropertyChanged(nameof(SuperLuckyBonusMultiplierText));
        OnPropertyChanged(nameof(LuckAdjustmentOffsetText));
        OnPropertyChanged(nameof(ValidationText));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
    }

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

internal sealed class RoosterDebugDraft
{
    public bool EnableDebug { get; set; }
    public bool VerboseLogging { get; set; }
    public bool TestingLogging { get; set; }
    public string BonusCalculationMultiplierText { get; set; } = string.Empty;
    public string SuperUnluckyBonusMultiplierText { get; set; } = string.Empty;
    public string SuperLuckyBonusMultiplierText { get; set; } = string.Empty;
    public string LuckAdjustmentOffsetText { get; set; } = string.Empty;

    public static RoosterDebugDraft FromConfig(RoosterControlConfig config)
        => new()
        {
            EnableDebug = config.General.EnableDebug,
            VerboseLogging = config.VerboseLogging,
            TestingLogging = config.TestingLogging,
#if RCTRL_DEV
            BonusCalculationMultiplierText = config.DebugKnobs.BonusCalculationMultiplier.ToString(CultureInfo.InvariantCulture),
            SuperUnluckyBonusMultiplierText = config.DebugKnobs.SuperUnluckyBonusMultiplier.ToString(CultureInfo.InvariantCulture),
            SuperLuckyBonusMultiplierText = config.DebugKnobs.SuperLuckyBonusMultiplier.ToString(CultureInfo.InvariantCulture),
            LuckAdjustmentOffsetText = config.DebugKnobs.LuckAdjustmentOffset.ToString(CultureInfo.InvariantCulture),
#else
            BonusCalculationMultiplierText = "0",
            SuperUnluckyBonusMultiplierText = "0",
            SuperLuckyBonusMultiplierText = "0",
            LuckAdjustmentOffsetText = "0",
#endif
        };

    public RoosterDebugDraft Clone()
        => new()
        {
            EnableDebug = EnableDebug,
            VerboseLogging = VerboseLogging,
            TestingLogging = TestingLogging,
            BonusCalculationMultiplierText = BonusCalculationMultiplierText,
            SuperUnluckyBonusMultiplierText = SuperUnluckyBonusMultiplierText,
            SuperLuckyBonusMultiplierText = SuperLuckyBonusMultiplierText,
            LuckAdjustmentOffsetText = LuckAdjustmentOffsetText,
        };

    public bool HasChangesComparedTo(RoosterDebugDraft other)
        => other is null
            || EnableDebug != other.EnableDebug
            || VerboseLogging != other.VerboseLogging
            || TestingLogging != other.TestingLogging
            || !string.Equals(BonusCalculationMultiplierText, other.BonusCalculationMultiplierText, StringComparison.Ordinal)
            || !string.Equals(SuperUnluckyBonusMultiplierText, other.SuperUnluckyBonusMultiplierText, StringComparison.Ordinal)
            || !string.Equals(SuperLuckyBonusMultiplierText, other.SuperLuckyBonusMultiplierText, StringComparison.Ordinal)
            || !string.Equals(LuckAdjustmentOffsetText, other.LuckAdjustmentOffsetText, StringComparison.Ordinal)
            ;

    public void ApplyTo(RoosterControlConfig config)
    {
        config.General.EnableDebug = EnableDebug;
        config.VerboseLogging = VerboseLogging;
        config.TestingLogging = TestingLogging;
#if RCTRL_DEV
        config.DebugKnobs.BonusCalculationMultiplier = float.Parse(BonusCalculationMultiplierText, CultureInfo.InvariantCulture);
        config.DebugKnobs.SuperUnluckyBonusMultiplier = float.Parse(SuperUnluckyBonusMultiplierText, CultureInfo.InvariantCulture);
        config.DebugKnobs.SuperLuckyBonusMultiplier = float.Parse(SuperLuckyBonusMultiplierText, CultureInfo.InvariantCulture);
        config.DebugKnobs.LuckAdjustmentOffset = float.Parse(LuckAdjustmentOffsetText, CultureInfo.InvariantCulture);
#endif
    }
}
