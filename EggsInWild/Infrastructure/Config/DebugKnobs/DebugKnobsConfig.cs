#if WEGG_DEV
namespace EggsInWild.Infrastructure.Config;

public sealed class DebugKnobsConfig
{
    public DesertTombKnobs DesertTomb { get; set; } = new();
    public SpawningChanceModifiersKnobs SpawningChanceModifiers { get; set; } = new();
    public WeightingKnobs Weighting { get; set; } = new();
    public MonstersKnobs Monsters { get; set; } = new();
}
#endif

