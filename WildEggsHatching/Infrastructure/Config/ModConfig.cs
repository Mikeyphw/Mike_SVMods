namespace WildEggsHatching.Infrastructure.Config;

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


    public TombChance TombChance { get; set; } = TombChance.Medium;

    public bool AlternatePotionSprite { get; set; } = false;
    public bool NeedsFern { get; set; } = true;
    public bool HatchDinoEggs { get; set; } = true;
    public bool DinoEggsExclusiveHatch { get; set; } = false;

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
}
