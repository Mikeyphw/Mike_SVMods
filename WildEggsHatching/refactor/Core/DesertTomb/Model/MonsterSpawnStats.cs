namespace WildEggsHatching.DesertTomb.Core.Model;

/// <summary>
/// Spawn-tuning statistics for a monster type within a tomb area.
/// </summary>
public sealed class MonsterSpawnStats
{
    public int Max { get; init; }
    public int Difficulty { get; init; }
    public int Speed { get; init; }

    public MonsterSpawnStats(int max, int difficulty, int speed)
    {
        Max = max;
        Difficulty = difficulty;
        Speed = speed;
    }

    /// <summary>
    /// Create a deep clone of this instance.
    /// </summary>
    public MonsterSpawnStats Clone() => new(Max, Difficulty, Speed);
}
