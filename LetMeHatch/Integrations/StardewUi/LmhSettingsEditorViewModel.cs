using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using LetMeHatch.Config;
using StardewValley.GameData.FarmAnimals;
using static LetMeHatch.EnumsAndStrings.Enums;

namespace LetMeHatch.Integrations.StardewUi;

public sealed class LmhSettingsEditorViewModel : INotifyPropertyChanged
{
    private readonly Func<ModConfig> _getConfig;
    private readonly Action<LmhSettingsDraft> _saveDraft;
    private readonly Func<IReadOnlyList<LmhSettingsAnimalOption>> _loadAnimals;
    private readonly Func<string> _breedingAvailabilityText;
    private readonly Func<string> _colorsAvailabilityText;
    private readonly Func<string> _mappingsAvailabilityText;

    private LmhSettingsDraft _snapshot = LmhSettingsDraft.CreateEmpty();
    private LmhSettingsDraft _draft = LmhSettingsDraft.CreateEmpty();
    private IReadOnlyList<LmhSettingsAnimalOption> _animals = Array.Empty<LmhSettingsAnimalOption>();
    private string? _selectedAnimalKey;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LmhSettingsEditorViewModel(
        Func<ModConfig> getConfig,
        Action<LmhSettingsDraft> saveDraft,
        Func<IReadOnlyList<LmhSettingsAnimalOption>> loadAnimals,
        Func<string> breedingAvailabilityText,
        Func<string> colorsAvailabilityText,
        Func<string> mappingsAvailabilityText)
    {
        _getConfig = getConfig ?? throw new ArgumentNullException(nameof(getConfig));
        _saveDraft = saveDraft ?? throw new ArgumentNullException(nameof(saveDraft));
        _loadAnimals = loadAnimals ?? throw new ArgumentNullException(nameof(loadAnimals));
        _breedingAvailabilityText = breedingAvailabilityText ?? throw new ArgumentNullException(nameof(breedingAvailabilityText));
        _colorsAvailabilityText = colorsAvailabilityText ?? throw new ArgumentNullException(nameof(colorsAvailabilityText));
        _mappingsAvailabilityText = mappingsAvailabilityText ?? throw new ArgumentNullException(nameof(mappingsAvailabilityText));
        Reload();
    }

    public string SummaryText => $"{AnimalOptions.Count} animals | Clone vanilla eggs: {(CloneVanillaEggs ? "On" : "Off")} | Breeding system: {(BreedingSystemEnabled ? "On" : "Off")}";
    public string BreedingKeybindText => _draft.BreedingKeybindText;
    public string ColorsKeybindText => _draft.ColorsKeybindText;
    public string MappingsKeybindText => _draft.MappingsKeybindText;
    public string BreedingAvailabilityText => _breedingAvailabilityText();
    public string ColorsAvailabilityText => _colorsAvailabilityText();
    public string MappingsAvailabilityText => _mappingsAvailabilityText();

    public bool EditEggListByName { get => _draft.EditEggListByName; set => SetBool(value, _draft.EditEggListByName, v => _draft.EditEggListByName = v, nameof(EditEggListByName)); }
    public bool EditBirthText { get => _draft.EditBirthText; set => SetBool(value, _draft.EditBirthText, v => _draft.EditBirthText = v, nameof(EditBirthText)); }
    public bool AlternateBirthText { get => _draft.AlternateBirthText; set => SetBool(value, _draft.AlternateBirthText, v => _draft.AlternateBirthText = v, nameof(AlternateBirthText)); }
    public bool CloneVanillaEggs { get => _draft.CloneVanillaEggs; set => SetBool(value, _draft.CloneVanillaEggs, v => _draft.CloneVanillaEggs = v, nameof(CloneVanillaEggs)); }
    public bool DinoEggAsArtifact { get => _draft.DinoEggAsArtifact; set => SetBool(value, _draft.DinoEggAsArtifact, v => _draft.DinoEggAsArtifact = v, nameof(DinoEggAsArtifact)); }
    public bool CloneMachineRules { get => _draft.CloneMachineRules; set => SetBool(value, _draft.CloneMachineRules, v => _draft.CloneMachineRules = v, nameof(CloneMachineRules)); }
    public bool UseDynamicCloneTextures { get => _draft.UseDynamicCloneTextures; set => SetBool(value, _draft.UseDynamicCloneTextures, v => _draft.UseDynamicCloneTextures = v, nameof(UseDynamicCloneTextures)); }
    public bool OnlyFertilizedEggs { get => _draft.OnlyFertilizedEggs; set => SetBool(value, _draft.OnlyFertilizedEggs, v => _draft.OnlyFertilizedEggs = v, nameof(OnlyFertilizedEggs)); }
    public bool ColorClonedMilk { get => _draft.ColorClonedMilk; set => SetBool(value, _draft.ColorClonedMilk, v => _draft.ColorClonedMilk = v, nameof(ColorClonedMilk)); }
    public bool ExclusiveRabbitWool { get => _draft.ExclusiveRabbitWool; set => SetBool(value, _draft.ExclusiveRabbitWool, v => _draft.ExclusiveRabbitWool = v, nameof(ExclusiveRabbitWool)); }
    public bool BreedingSystemEnabled { get => _draft.BreedingSystemEnabled; set => SetBool(value, _draft.BreedingSystemEnabled, v => _draft.BreedingSystemEnabled = v, nameof(BreedingSystemEnabled)); }
    public bool DeterministicBreedingRng { get => _draft.DeterministicBreedingRng; set => SetBool(value, _draft.DeterministicBreedingRng, v => _draft.DeterministicBreedingRng = v, nameof(DeterministicBreedingRng)); }

    public IReadOnlyList<string> DynamicCloneTintModeOptions => Enum.GetNames(typeof(DynamicCloneTintMode));
    public string SelectedDynamicCloneTintMode
    {
        get => _draft.DynamicCloneTintMode.ToString();
        set
        {
            if (!Enum.TryParse<DynamicCloneTintMode>(value, true, out var parsed) || _draft.DynamicCloneTintMode == parsed)
                return;
            _draft.DynamicCloneTintMode = parsed;
            MarkDirty(nameof(SelectedDynamicCloneTintMode));
        }
    }

    public string DailyLuckBonusText
    {
        get => _draft.DailyLuckBonusText;
        set
        {
            if (_draft.DailyLuckBonusText == value)
                return;
            _draft.DailyLuckBonusText = value;
            MarkDirty(nameof(DailyLuckBonusText), nameof(ValidationText));
        }
    }

    public string NormalEggChanceText
    {
        get => _draft.NormalEggChanceText;
        set
        {
            if (_draft.NormalEggChanceText == value)
                return;
            _draft.NormalEggChanceText = value;
            MarkDirty(nameof(NormalEggChanceText), nameof(ValidationText));
        }
    }

    public string LargeEggChanceText
    {
        get => _draft.LargeEggChanceText;
        set
        {
            if (_draft.LargeEggChanceText == value)
                return;
            _draft.LargeEggChanceText = value;
            MarkDirty(nameof(LargeEggChanceText), nameof(ValidationText));
        }
    }

    public string FertilizedEggChanceText
    {
        get => _draft.FertilizedEggChanceText;
        set
        {
            if (_draft.FertilizedEggChanceText == value)
                return;
            _draft.FertilizedEggChanceText = value;
            MarkDirty(nameof(FertilizedEggChanceText), nameof(ValidationText));
        }
    }

    public IReadOnlyList<LmhSettingsAnimalOption> AnimalOptions => _animals;
    public IReadOnlyList<string> AnimalOptionKeys => _animals.Select(a => a.Key).ToList();
    public string SelectedAnimalKey
    {
        get => _selectedAnimalKey ?? string.Empty;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(_selectedAnimalKey, value, StringComparison.OrdinalIgnoreCase))
                return;
            _selectedAnimalKey = value;
            NotifyAnimalSelection();
        }
    }

    public string SelectedAnimalLabel => _animals.FirstOrDefault(a => string.Equals(a.Key, _selectedAnimalKey, StringComparison.OrdinalIgnoreCase))?.Label ?? "Select an animal";
    public bool EnableEggListEdit
    {
        get => GetAnimalOverride(EditType.EggList);
        set => SetAnimalOverride(EditType.EggList, value, nameof(EnableEggListEdit));
    }

    public bool EnableBirthTextEdit
    {
        get => GetAnimalOverride(EditType.BirthText);
        set => SetAnimalOverride(EditType.BirthText, value, nameof(EnableBirthTextEdit));
    }

    public bool EnableCloneEggsEdit
    {
        get => GetAnimalOverride(EditType.CloneEggs);
        set => SetAnimalOverride(EditType.CloneEggs, value, nameof(EnableCloneEggsEdit));
    }

    public string ValidationText => BuildValidationText();
    public bool CanSave => _draft.HasChangesComparedTo(_snapshot) && string.IsNullOrEmpty(ValidationText);
    public bool CanDiscard => _draft.HasChangesComparedTo(_snapshot);

    public void Save()
    {
        if (!CanSave)
            return;

        _saveDraft(_draft.Clone());
        _snapshot = LmhSettingsDraft.FromConfig(_getConfig());
        _draft = _snapshot.Clone();
        _animals = _loadAnimals();
        EnsureSelectedAnimal();
        NotifyAll();
    }

    public void Discard()
    {
        _draft = _snapshot.Clone();
        NotifyAll();
    }

    public void Reload()
    {
        _snapshot = LmhSettingsDraft.FromConfig(_getConfig());
        _draft = _snapshot.Clone();
        _animals = _loadAnimals();
        EnsureSelectedAnimal();
        NotifyAll();
    }

    private void SetBool(bool value, bool current, Action<bool> set, string propertyName)
    {
        if (value == current)
            return;
        set(value);
        MarkDirty(propertyName);
    }

    private bool GetAnimalOverride(EditType editType)
    {
        if (string.IsNullOrWhiteSpace(_selectedAnimalKey))
            return true;
        return !GetOverrides(editType).TryGetValue(_selectedAnimalKey, out var value) || value;
    }

    private void SetAnimalOverride(EditType editType, bool enabled, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(_selectedAnimalKey))
            return;

        var overrides = GetOverrides(editType);
        if (enabled)
            overrides.Remove(_selectedAnimalKey);
        else
            overrides[_selectedAnimalKey] = false;

        MarkDirty(propertyName);
    }

    private Dictionary<string, bool> GetOverrides(EditType editType)
        => editType switch
        {
            EditType.EggList => _draft.EditedEggListsOptions,
            EditType.BirthText => _draft.EditedBirthTextOptions,
            EditType.CloneEggs => _draft.ClonedVanillaEggsOptions,
            _ => throw new ArgumentOutOfRangeException(nameof(editType), editType, null)
        };

    private string BuildValidationText()
    {
        if (!TryParseChance(_draft.DailyLuckBonusText, out _))
            return "Daily luck bonus must be a number from 0 to 1.";
        if (!TryParseChance(_draft.NormalEggChanceText, out _))
            return "Normal egg chance must be a number from 0 to 1.";
        if (!TryParseChance(_draft.LargeEggChanceText, out _))
            return "Large egg chance must be a number from 0 to 1.";
        if (!TryParseChance(_draft.FertilizedEggChanceText, out _))
            return "Fertilized egg chance must be a number from 0 to 1.";

        return string.Empty;
    }

    private static bool TryParseChance(string raw, out float value)
    {
        if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return false;
        return value >= 0f && value <= 1f;
    }

    private void EnsureSelectedAnimal()
    {
        if (_animals.Count == 0)
        {
            _selectedAnimalKey = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedAnimalKey) || !_animals.Any(a => string.Equals(a.Key, _selectedAnimalKey, StringComparison.OrdinalIgnoreCase)))
            _selectedAnimalKey = _animals[0].Key;
    }

    private void NotifyAnimalSelection()
    {
        OnPropertyChanged(nameof(SelectedAnimalKey));
        OnPropertyChanged(nameof(SelectedAnimalLabel));
        OnPropertyChanged(nameof(EnableEggListEdit));
        OnPropertyChanged(nameof(EnableBirthTextEdit));
        OnPropertyChanged(nameof(EnableCloneEggsEdit));
    }

    private void NotifyAll()
    {
        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(BreedingKeybindText));
        OnPropertyChanged(nameof(ColorsKeybindText));
        OnPropertyChanged(nameof(MappingsKeybindText));
        OnPropertyChanged(nameof(BreedingAvailabilityText));
        OnPropertyChanged(nameof(ColorsAvailabilityText));
        OnPropertyChanged(nameof(MappingsAvailabilityText));
        OnPropertyChanged(nameof(EditEggListByName));
        OnPropertyChanged(nameof(EditBirthText));
        OnPropertyChanged(nameof(AlternateBirthText));
        OnPropertyChanged(nameof(CloneVanillaEggs));
        OnPropertyChanged(nameof(DinoEggAsArtifact));
        OnPropertyChanged(nameof(CloneMachineRules));
        OnPropertyChanged(nameof(UseDynamicCloneTextures));
        OnPropertyChanged(nameof(OnlyFertilizedEggs));
        OnPropertyChanged(nameof(ColorClonedMilk));
        OnPropertyChanged(nameof(ExclusiveRabbitWool));
        OnPropertyChanged(nameof(BreedingSystemEnabled));
        OnPropertyChanged(nameof(DeterministicBreedingRng));
        OnPropertyChanged(nameof(SelectedDynamicCloneTintMode));
        OnPropertyChanged(nameof(DailyLuckBonusText));
        OnPropertyChanged(nameof(NormalEggChanceText));
        OnPropertyChanged(nameof(LargeEggChanceText));
        OnPropertyChanged(nameof(FertilizedEggChanceText));
        OnPropertyChanged(nameof(AnimalOptions));
        OnPropertyChanged(nameof(AnimalOptionKeys));
        NotifyAnimalSelection();
        OnPropertyChanged(nameof(ValidationText));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
    }

    private void MarkDirty(params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
            OnPropertyChanged(propertyName);

        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
    }

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class LmhDebugEditorViewModel : INotifyPropertyChanged
{
    private readonly Func<ModConfig> _getConfig;
    private readonly Action<LmhDebugDraft> _saveDraft;
    private LmhDebugDraft _snapshot = LmhDebugDraft.CreateEmpty();
    private LmhDebugDraft _draft = LmhDebugDraft.CreateEmpty();

    public event PropertyChangedEventHandler? PropertyChanged;

    public LmhDebugEditorViewModel(Func<ModConfig> getConfig, Action<LmhDebugDraft> saveDraft)
    {
        _getConfig = getConfig ?? throw new ArgumentNullException(nameof(getConfig));
        _saveDraft = saveDraft ?? throw new ArgumentNullException(nameof(saveDraft));
        Reload();
    }

    public bool ShowEdits { get => _draft.ShowEdits; set => SetBool(value, _draft.ShowEdits, v => _draft.ShowEdits = v, nameof(ShowEdits)); }
    public bool ShowAnimalKeys { get => _draft.ShowAnimalKeys; set => SetBool(value, _draft.ShowAnimalKeys, v => _draft.ShowAnimalKeys = v, nameof(ShowAnimalKeys)); }
    public bool VerboseLogging { get => _draft.VerboseLogging; set => SetBool(value, _draft.VerboseLogging, v => _draft.VerboseLogging = v, nameof(VerboseLogging)); }
    public bool TestingLogging { get => _draft.TestingLogging; set => SetBool(value, _draft.TestingLogging, v => _draft.TestingLogging = v, nameof(TestingLogging)); }
    public bool CanSave => _draft.HasChangesComparedTo(_snapshot);
    public bool CanDiscard => _draft.HasChangesComparedTo(_snapshot);

    public void Save()
    {
        if (!CanSave)
            return;
        _saveDraft(_draft);
        _snapshot = LmhDebugDraft.FromConfig(_getConfig());
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
        _snapshot = LmhDebugDraft.FromConfig(_getConfig());
        _draft = _snapshot.Clone();
        NotifyAll();
    }

    private void SetBool(bool value, bool current, Action<bool> set, string propertyName)
    {
        if (value == current)
            return;
        set(value);
        OnPropertyChanged(propertyName);
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
    }

    private void NotifyAll()
    {
        OnPropertyChanged(nameof(ShowEdits));
        OnPropertyChanged(nameof(ShowAnimalKeys));
        OnPropertyChanged(nameof(VerboseLogging));
        OnPropertyChanged(nameof(TestingLogging));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
    }

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record LmhSettingsAnimalOption(string Key, string Label);

public sealed class LmhSettingsDraft
{
    public bool EditEggListByName { get; set; }
    public bool EditBirthText { get; set; }
    public bool AlternateBirthText { get; set; }
    public bool CloneVanillaEggs { get; set; }
    public bool DinoEggAsArtifact { get; set; }
    public bool CloneMachineRules { get; set; }
    public bool UseDynamicCloneTextures { get; set; }
    public DynamicCloneTintMode DynamicCloneTintMode { get; set; }
    public bool OnlyFertilizedEggs { get; set; }
    public bool ColorClonedMilk { get; set; }
    public bool ExclusiveRabbitWool { get; set; }
    public bool BreedingSystemEnabled { get; set; }
    public bool DeterministicBreedingRng { get; set; }
    public string DailyLuckBonusText { get; set; } = "0";
    public string NormalEggChanceText { get; set; } = "0";
    public string LargeEggChanceText { get; set; } = "0";
    public string FertilizedEggChanceText { get; set; } = "0";
    public string BreedingKeybindText { get; set; } = string.Empty;
    public string ColorsKeybindText { get; set; } = string.Empty;
    public string MappingsKeybindText { get; set; } = string.Empty;
    public Dictionary<string, bool> EditedEggListsOptions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, bool> EditedBirthTextOptions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, bool> ClonedVanillaEggsOptions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static LmhSettingsDraft CreateEmpty() => new();

    public static LmhSettingsDraft FromConfig(ModConfig config)
        => new()
        {
            EditEggListByName = config.EditEggListByName,
            EditBirthText = config.EditBirthText,
            AlternateBirthText = config.AlternateBirthText,
            CloneVanillaEggs = config.CloneVanillaEggs,
            DinoEggAsArtifact = config.DinoEggAsArtifact,
            CloneMachineRules = config.CloneMachineRules,
            UseDynamicCloneTextures = config.UseDynamicCloneTextures,
            DynamicCloneTintMode = config.DynamicCloneTintMode,
            OnlyFertilizedEggs = config.OnlyFertilizedEggs,
            ColorClonedMilk = config.ColorClonedMilk,
            ExclusiveRabbitWool = config.ExclusiveRabbitWool,
            BreedingSystemEnabled = config.BreedingSystemEnabled,
            DeterministicBreedingRng = config.DeterministicBreedingRng,
            DailyLuckBonusText = config.DailyLuckBonus.ToString(CultureInfo.InvariantCulture),
            NormalEggChanceText = GetEggChance(config, ItemType.Normal).ToString(CultureInfo.InvariantCulture),
            LargeEggChanceText = GetEggChance(config, ItemType.Large).ToString(CultureInfo.InvariantCulture),
            FertilizedEggChanceText = GetEggChance(config, ItemType.FertilizedEgg).ToString(CultureInfo.InvariantCulture),
            BreedingKeybindText = config.OpenBreedingEditorKey.ToString(),
            ColorsKeybindText = config.OpenColorsEditorKey.ToString(),
            MappingsKeybindText = config.OpenAdvancedMappingsEditorKey.ToString(),
            EditedEggListsOptions = new Dictionary<string, bool>(config.EditedEggListsOptions, StringComparer.OrdinalIgnoreCase),
            EditedBirthTextOptions = new Dictionary<string, bool>(config.EditedBirthTextOptions, StringComparer.OrdinalIgnoreCase),
            ClonedVanillaEggsOptions = new Dictionary<string, bool>(config.ClonedVanillaEggsOptions, StringComparer.OrdinalIgnoreCase),
        };

    public LmhSettingsDraft Clone()
        => new()
        {
            EditEggListByName = EditEggListByName,
            EditBirthText = EditBirthText,
            AlternateBirthText = AlternateBirthText,
            CloneVanillaEggs = CloneVanillaEggs,
            DinoEggAsArtifact = DinoEggAsArtifact,
            CloneMachineRules = CloneMachineRules,
            UseDynamicCloneTextures = UseDynamicCloneTextures,
            DynamicCloneTintMode = DynamicCloneTintMode,
            OnlyFertilizedEggs = OnlyFertilizedEggs,
            ColorClonedMilk = ColorClonedMilk,
            ExclusiveRabbitWool = ExclusiveRabbitWool,
            BreedingSystemEnabled = BreedingSystemEnabled,
            DeterministicBreedingRng = DeterministicBreedingRng,
            DailyLuckBonusText = DailyLuckBonusText,
            NormalEggChanceText = NormalEggChanceText,
            LargeEggChanceText = LargeEggChanceText,
            FertilizedEggChanceText = FertilizedEggChanceText,
            BreedingKeybindText = BreedingKeybindText,
            ColorsKeybindText = ColorsKeybindText,
            MappingsKeybindText = MappingsKeybindText,
            EditedEggListsOptions = new Dictionary<string, bool>(EditedEggListsOptions, StringComparer.OrdinalIgnoreCase),
            EditedBirthTextOptions = new Dictionary<string, bool>(EditedBirthTextOptions, StringComparer.OrdinalIgnoreCase),
            ClonedVanillaEggsOptions = new Dictionary<string, bool>(ClonedVanillaEggsOptions, StringComparer.OrdinalIgnoreCase),
        };

    public bool HasChangesComparedTo(LmhSettingsDraft other)
        => other is null
            || EditEggListByName != other.EditEggListByName
            || EditBirthText != other.EditBirthText
            || AlternateBirthText != other.AlternateBirthText
            || CloneVanillaEggs != other.CloneVanillaEggs
            || DinoEggAsArtifact != other.DinoEggAsArtifact
            || CloneMachineRules != other.CloneMachineRules
            || UseDynamicCloneTextures != other.UseDynamicCloneTextures
            || DynamicCloneTintMode != other.DynamicCloneTintMode
            || OnlyFertilizedEggs != other.OnlyFertilizedEggs
            || ColorClonedMilk != other.ColorClonedMilk
            || ExclusiveRabbitWool != other.ExclusiveRabbitWool
            || BreedingSystemEnabled != other.BreedingSystemEnabled
            || DeterministicBreedingRng != other.DeterministicBreedingRng
            || !string.Equals(DailyLuckBonusText, other.DailyLuckBonusText, StringComparison.Ordinal)
            || !string.Equals(NormalEggChanceText, other.NormalEggChanceText, StringComparison.Ordinal)
            || !string.Equals(LargeEggChanceText, other.LargeEggChanceText, StringComparison.Ordinal)
            || !string.Equals(FertilizedEggChanceText, other.FertilizedEggChanceText, StringComparison.Ordinal)
            || !EditedEggListsOptions.OrderBy(p => p.Key).SequenceEqual(other.EditedEggListsOptions.OrderBy(p => p.Key))
            || !EditedBirthTextOptions.OrderBy(p => p.Key).SequenceEqual(other.EditedBirthTextOptions.OrderBy(p => p.Key))
            || !ClonedVanillaEggsOptions.OrderBy(p => p.Key).SequenceEqual(other.ClonedVanillaEggsOptions.OrderBy(p => p.Key));

    private static float GetEggChance(ModConfig config, ItemType type)
        => config.EggChances.TryGetValue(type, out var value) ? value : 0f;
}

public sealed class LmhDebugDraft
{
    public bool ShowEdits { get; set; }
    public bool ShowAnimalKeys { get; set; }
    public bool VerboseLogging { get; set; }
    public bool TestingLogging { get; set; }

    public static LmhDebugDraft CreateEmpty() => new();

    public static LmhDebugDraft FromConfig(ModConfig config)
        => new()
        {
            ShowEdits = config.ShowEdits,
            ShowAnimalKeys = config.ShowAnimalKeys,
            VerboseLogging = config.VerboseLogging,
            TestingLogging = config.TestingLogging,
        };

    public LmhDebugDraft Clone()
        => new()
        {
            ShowEdits = ShowEdits,
            ShowAnimalKeys = ShowAnimalKeys,
            VerboseLogging = VerboseLogging,
            TestingLogging = TestingLogging,
        };

    public bool HasChangesComparedTo(LmhDebugDraft other)
        => other is null
            || ShowEdits != other.ShowEdits
            || ShowAnimalKeys != other.ShowAnimalKeys
            || VerboseLogging != other.VerboseLogging
            || TestingLogging != other.TestingLogging;
}
