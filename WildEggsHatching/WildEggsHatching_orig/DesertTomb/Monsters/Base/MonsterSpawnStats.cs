namespace WildEggsHatching.DesertTomb.Monsters.Base
{
    internal sealed class MonsterSpawnStats
    {
        internal int Max { get; set; }
        internal int Difficulty { get; set; }
        internal int Speed { get; set; }

        internal MonsterSpawnStats(int max, int difficulty, int speed)
        {
            Max = max;
            Difficulty = difficulty;
            Speed = speed;
        }

        /// <summary>
        /// Creates a deep clone of this MonsterSpawnStats instance
        /// </summary>
        internal MonsterSpawnStats Clone()
        {
            return new MonsterSpawnStats(Max, Difficulty, Speed);
        }
    }
}
