namespace WildEggsHatching.Infrastructure.Config;

public sealed class ModConfig
{
    public bool ProgressionMode { get; set; } = true;

    /// <summary>
    /// Whether the DesertTomb module is enabled. If disabled, progression will auto-skip DesertTomb-dependent steps.
    /// </summary>
    public bool DesertTombModuleEnabled { get; set; } = true;

    public TombChance TombChance { get; set; } = TombChance.Medium;

    public bool AlternatePotionSprite { get; set; } = false;
    public bool NeedsFern { get; set; } = true;
    public bool HatchDinoEggs { get; set; } = true;
    public bool DinoEggsExclusiveHatch { get; set; } = false;

    public string DinoPrice { get; set; } = "6000";
    public string VoidChickenPrice { get; set; } = "default";
    public string OstrichPrice { get; set; } = "500";
}
