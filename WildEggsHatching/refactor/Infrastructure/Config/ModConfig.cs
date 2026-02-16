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
}