#if WEGG_DEV
namespace EggsInWild.Infrastructure.Config;

public sealed class DesertTombSpawningKnobs
{
    public float GuardFraction { get; set; } = 0.7f;
    public float WanderPackChance { get; set; } = 0.6f;

    public int LeashRadiusMin { get; set; } = 6;
    public int LeashRadiusMax { get; set; } = 20;

    public int PatrolPointsMinInclusive { get; set; } = 2;
    public int PatrolPointsMaxExclusive { get; set; } = 5;

    public int PatrolMaxAttempts { get; set; } = 200;
    public int PatrolAttemptsPerCandidate { get; set; } = 2;
    public int PatrolMinSpacingTiles { get; set; } = 2;
}
#endif

