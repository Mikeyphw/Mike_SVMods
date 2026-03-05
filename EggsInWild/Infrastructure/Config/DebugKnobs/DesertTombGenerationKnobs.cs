#if WEGG_DEV
namespace EggsInWild.Infrastructure.Config;

public sealed class DesertTombGenerationKnobs
{
    public float LightingDarkChance { get; set; } = 0.3f;
    public float LightingDarkAltChance { get; set; } = 0.3f;
    public float LightingSpecialDarkChance { get; set; } = 0.15f;

    public int BreakablesBaseNextMaxExclusive { get; set; } = 5;
    public float BreakablesLuckScalar { get; set; } = 20.0f;
    public int BreakablesMapAreaDivisor { get; set; } = 40;

    public float BreakablesEdgeTopChance { get; set; } = 0.33f;
    public int BreakablesPlacementGuardMax { get; set; } = 512;
    public float BreakablesPlaceChance { get; set; } = 0.25f;
    public float BreakablesClusterChance { get; set; } = 0.35f;

    public int LightsMapAreaDivisor { get; set; } = 220;
    public int LightsTargetMin { get; set; } = 10;
    public int LightsTargetMax { get; set; } = 40;
    public int LightsAttemptsMultiplier { get; set; } = 30;
}
#endif

