namespace WildEggsHatching.Infrastructure.Config;

/// <summary>
/// Controls how many DesertTomb floors are registered into the AdditionalMineMaps dictionary.
/// This mirrors the legacy mod's semantics.
/// </summary>
public enum TombChance
{
    Low,
    Medium,
    High,
    Always,
}
