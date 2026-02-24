namespace EggsInWild.Infrastructure.Config;

public sealed class ModConfig
{
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


    /// <summary>
    /// If enabled, DesertTomb floors can still appear after the DesertTomb progression phase is complete.
    /// This mirrors the original mod's "KeepMineLevel" behavior.
    /// </summary>
    public bool KeepMineLevel { get; set; } = false;

    /// <summary>
    /// If enabled, forces the DesertTomb layout selection at chooseLevelType time, preventing vanilla from drifting into another layout.
    /// </summary>
    public bool DesertTombForcePatch { get; set; } = false;

    /// <summary>
    /// If enabled alongside DesertTombForcePatch, bypasses progression gate checks when applying forced tomb layouts.
    /// </summary>
    public bool DesertTombIgnoreProgressionGate { get; set; } = false;

    public TombChance TombChance { get; set; } = TombChance.Medium;

    public TombDifficulty TombDifficulty { get; set; } = TombDifficulty.Moderate;

    public bool AlternatePotionSprite { get; set; } = false;
    public bool NeedsFern { get; set; } = true;
    public bool FruitInsteadOfSeed { get; set; } = false;

    public bool HatchDinoEggs { get; set; } = true;
    public bool DinoEggsExclusiveHatch { get; set; } = true;
    public string BirthText { get; set; } = "Default";

    public string DinoPrice { get; set; } = "6000";
    public string VoidChickenPrice { get; set; } = "default";
    public string OstrichPrice { get; set; } = "500";


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



    /// <summary>
    /// Per-animal progression gating for shop availability.
    /// True = locked by default, becomes available after AnimalNotes unlock mail.
    /// False = never gated by AnimalNotes (always follow base game/mod shop behavior).
    /// </summary>
    public System.Collections.Generic.Dictionary<string, bool> AnimalProgressionMode { get; set; } = new(System.StringComparer.OrdinalIgnoreCase);

#if MIKE_DEVTOOLS
    /// <summary>
    /// Centralized tuning knobs exposed only in debug builds (GMCM Debug page).
    /// </summary>
    public DebugKnobsConfig DebugKnobs { get; set; } = new();
#endif
}
