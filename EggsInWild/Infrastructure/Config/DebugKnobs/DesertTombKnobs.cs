#if WEGG_DEV
namespace EggsInWild.Infrastructure.Config;

public sealed class DesertTombKnobs
{
    public DesertTombGenerationKnobs Generation { get; set; } = new();
    public DesertTombSpawningKnobs Spawning { get; set; } = new();
    public DesertTombPopulationKnobs Population { get; set; } = new();
    public DesertTombLootLuckKnobs LootLuck { get; set; } = new();
    public DesertTombBreakablesKnobs Breakables { get; set; } = new();
}
#endif

