namespace WildEggsHatching.Infrastructure;

/// <summary>
/// Interface for accessing mod configuration.
/// This avoids direct ModEntry dependency in infrastructure/services.
/// </summary>
public interface IConfigProvider
{
    /// <summary>Gets the current config instance.</summary>
    ModConfig Config { get; }
}

/// <summary>
/// Placeholder for mod config - to be replaced with actual config class.
/// </summary>
public class ModConfig
{
    /// <summary>Animal progression mode settings.</summary>
    public Dictionary<string, bool> AnimalProgressionMode { get; set; } = new();

    /// <summary>Price config for Dinosaur.</summary>
    public string DinoPrice { get; set; } = "6000";

    /// <summary>Price config for Void Chicken.</summary>
    public string VoidChickenPrice { get; set; } = "default";

    /// <summary>Price config for Ostrich.</summary>
    public string OstrichPrice { get; set; } = "500";

    /// <summary>Whether progression mode is enabled.</summary>
    public bool ProgressionMode { get; set; } = true;

    /// <summary>Use alternate potion sprite.</summary>
    public bool AlternatePotionSprite { get; set; } = false;

    /// <summary>Whether the incubator requires ferns as fuel.</summary>
    public bool NeedsFern { get; set; } = true;

    /// <summary>Whether dinosaur eggs can hatch in the wild egg incubator.</summary>
    public bool HatchDinoEggs { get; set; } = true;

    /// <summary>Whether dinosaur eggs are exclusive to dino incubators.</summary>
    public bool DinoEggsExclusiveHatch { get; set; } = false;
}
