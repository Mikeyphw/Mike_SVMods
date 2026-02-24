namespace EggsInWild.DesertTomb.Core.Model;

/// <summary>
/// Spawn tuning for a monster type within a tomb area.
/// </summary>
/// <param name="Weight">How likely this type is chosen by the planner.</param>
/// <param name="MinPerArea">Minimum count for this type when selected (reserved for later).</param>
/// <param name="MaxPerArea">Maximum count for this type within a single area.</param>
/// <param name="PackMin">Minimum pack size when spawned as a pack (reserved for later).</param>
/// <param name="PackMax">Maximum pack size when spawned as a pack (reserved for later).</param>
/// <param name="DifficultyAdd">Additive difficulty modifier applied at monster creation time.</param>
/// <param name="SpeedAdd">Additive speed modifier applied at monster creation time.</param>
public sealed record MonsterSpawnStats(
    int Weight,
    int MinPerArea,
    int MaxPerArea,
    int PackMin,
    int PackMax,
    int DifficultyAdd,
    int SpeedAdd
);

public static class MonsterSpawnStatsExtensions
{
    public static MonsterSpawnStats Clone(this MonsterSpawnStats value) => value with { };
}
