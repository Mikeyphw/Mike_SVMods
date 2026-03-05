#if WEGG_DEV
namespace EggsInWild.Infrastructure.Config;

public sealed class DesertTombPopulationKnobs
{
    public float InitialWaveFraction { get; set; } = 0.75f;
    public int ReconcileIntervalTicks { get; set; } = 30;
    public int MaxSpawnsPerTick { get; set; } = 6;
    public int MinDistanceToPlayerTiles { get; set; } = 5;
    public bool AllowRespawnAfterInitialFill { get; set; } = false;
}
#endif

