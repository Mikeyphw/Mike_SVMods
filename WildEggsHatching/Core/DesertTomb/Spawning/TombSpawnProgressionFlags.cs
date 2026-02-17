using WildEggsHatching.Core.Enums;

namespace WildEggsHatching.DesertTomb.Core.Spawning;

/// <summary>
/// Progression context used by tomb spawning policies.
/// </summary>
public readonly record struct TombSpawnProgressionFlags(bool ProgressionModeEnabled, ProgressionStep Step)
{
    public static TombSpawnProgressionFlags Disabled => new(false, ProgressionStep.PartI);
}
