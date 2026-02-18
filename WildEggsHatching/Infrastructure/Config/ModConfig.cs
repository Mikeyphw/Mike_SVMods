using System.Collections.Generic;

namespace WildEggsHatching.Infrastructure.Config;

public enum UpdateRate
{
    DayStart,
    Hourly,
    TenMinutes,
}

public enum BirthText
{
    Default,
    Always,
    Never,
}

public enum TombDifficulty
{
    Easy,
    Moderate,
    Hard,
}

public sealed class ModConfig
{
    // -----------------------------
    // Phase 8+ config
    // -----------------------------
    public bool ProgressionMode { get; set; } = true;

    /// <summary>
    /// Whether the DesertTomb module is enabled. If disabled, progression will auto-skip DesertTomb-dependent steps.
    /// </summary>
    public bool DesertTombModuleEnabled { get; set; } = true;

    /// <summary>
    /// Render a HUD arrow pointing towards The Pharaoh when off-screen (debug/optional).
    /// </summary>
    public bool DesertTombPharaohTrackerEnabled { get; set; } = false;

    /// <summary>
    /// Enable extra DesertTomb trace logging (debug/optional).
    /// </summary>
    public bool DesertTombDebugLogging { get; set; } = false;

    public TombChance TombChance { get; set; } = TombChance.Medium;

    public bool AlternatePotionSprite { get; set; } = false;
    public bool NeedsFern { get; set; } = true;
    public bool HatchDinoEggs { get; set; } = true;
    public bool DinoEggsExclusiveHatch { get; set; } = false;

    public string DinoPrice { get; set; } = "6000";
    public string VoidChickenPrice { get; set; } = "default";
    public string OstrichPrice { get; set; } = "500";

    // -----------------------------
    // Legacy config (kept for GMCM parity / backwards compatibility)
    // -----------------------------
    public UpdateRate UpdateRate { get; set; } = UpdateRate.DayStart;

    public bool WildEggEdit { get; set; } = false;

    /// <summary>Per-animal weights used by legacy UI pages. The refactor currently does not consume these values directly.</summary>
    public Dictionary<string, float> ChosenWeights { get; set; } = new() { ["Chicken"] = 0.6f, ["Duck"] = 0.4f };

    public bool ShowCoopAnimal { get; set; } = false;
    public bool ShowBarnAnimal { get; set; } = false;
    public bool ShowMagicCoopAnimal { get; set; } = false;
    public bool ShowMagicBarnAnimal { get; set; } = false;

    /// <summary>Legacy per-animal progression toggles. Keys are wild animal ids.</summary>
    public Dictionary<string, bool> AnimalProgressionMode { get; set; } = new();

    public BirthText BirthText { get; set; } = BirthText.Default;

    public bool SetRandom { get; set; } = false;
    public bool SetZero { get; set; } = false;

    public bool KeepMineLevel { get; set; } = false;
    public bool FruitInsteadOfSeed { get; set; } = false;
    public bool EnableSpiders { get; set; } = true;

    public bool ExtraDebugInfo { get; set; } = false;
    public bool Testing { get; set; } = false;

    public TombDifficulty TombDifficulty { get; set; } = TombDifficulty.Moderate;

    // -----------------------------
    // AnimalNotes config
    // -----------------------------
    /// <summary>
    /// Enable extra AnimalNotes HUD warnings + trace logs (debug/optional).
    /// </summary>
    public bool AnimalNotesDebugMode { get; set; } = false;

    /// <summary>
    /// Show a warning HUD message after this many consecutive in-game days of placement failures.
    /// </summary>
    public int AnimalNotesWarnAfterDays { get; set; } = 2;

    /// <summary>
    /// Max search radius (tiles) when placing Junimo notes inside animal houses.
    /// </summary>
    public int AnimalNotesPlacementRadius { get; set; } = 8;

    /// <summary>
    /// In multiplayer: only the host may add quests and queue mail for AnimalNotes.
    /// If false, clients may also add quests/mail locally (not recommended unless you understand the sync implications).
    /// </summary>
    public bool AnimalNotesQuestMailHostOnly { get; set; } = true;

    /// <summary>
    /// In multiplayer: allow clients to send "note found" messages to the host so the host can issue the quest.
    /// </summary>
    public bool AnimalNotesAllowClientDiscoveries { get; set; } = true;

    public void Reset()
    {
        var defaults = new ModConfig();

        ProgressionMode = defaults.ProgressionMode;
        DesertTombModuleEnabled = defaults.DesertTombModuleEnabled;
        DesertTombPharaohTrackerEnabled = defaults.DesertTombPharaohTrackerEnabled;
        DesertTombDebugLogging = defaults.DesertTombDebugLogging;
        TombChance = defaults.TombChance;

        AlternatePotionSprite = defaults.AlternatePotionSprite;
        NeedsFern = defaults.NeedsFern;
        HatchDinoEggs = defaults.HatchDinoEggs;
        DinoEggsExclusiveHatch = defaults.DinoEggsExclusiveHatch;

        DinoPrice = defaults.DinoPrice;
        VoidChickenPrice = defaults.VoidChickenPrice;
        OstrichPrice = defaults.OstrichPrice;

        UpdateRate = defaults.UpdateRate;
        WildEggEdit = defaults.WildEggEdit;
        ChosenWeights = new Dictionary<string, float>(defaults.ChosenWeights);

        ShowCoopAnimal = defaults.ShowCoopAnimal;
        ShowBarnAnimal = defaults.ShowBarnAnimal;
        ShowMagicCoopAnimal = defaults.ShowMagicCoopAnimal;
        ShowMagicBarnAnimal = defaults.ShowMagicBarnAnimal;

        AnimalProgressionMode.Clear();

        BirthText = defaults.BirthText;
        SetRandom = defaults.SetRandom;
        SetZero = defaults.SetZero;

        KeepMineLevel = defaults.KeepMineLevel;
        FruitInsteadOfSeed = defaults.FruitInsteadOfSeed;
        EnableSpiders = defaults.EnableSpiders;

        ExtraDebugInfo = defaults.ExtraDebugInfo;
        Testing = defaults.Testing;

        TombDifficulty = defaults.TombDifficulty;

        AnimalNotesDebugMode = defaults.AnimalNotesDebugMode;
        AnimalNotesWarnAfterDays = defaults.AnimalNotesWarnAfterDays;
        AnimalNotesPlacementRadius = defaults.AnimalNotesPlacementRadius;
        AnimalNotesQuestMailHostOnly = defaults.AnimalNotesQuestMailHostOnly;
        AnimalNotesAllowClientDiscoveries = defaults.AnimalNotesAllowClientDiscoveries;
    }
}
