namespace WildEggsHatching.DesertTomb.Core.Enums;

/// <summary>
/// DesertTomb-specific enums used by configuration, layouts, and spawning.
/// </summary>
public static class DesertTombEnums
{
    public enum TombDifficulty
    {
        Easy,
        Moderate,
        Hard,
    }

    public enum TombAreaType
    {
        MummyArea,
        NormalArea,
        SmallArea,
        DuggyArea,
        MummyDefendArea,
    }

    public enum TombMonsterType
    {
        Mummy,
        Bat,
        Ghost,
        Duggy,
        AssassinBug,
        Spider,
        BruteMummy,
        Skeleton,
        SkeletonMage,
        HauntedSkull,
        RoyalSerpent,
        TheTombPharaoh,
    }

    /// <summary>
    /// Logical identifiers for the tomb mine maps used by DesertTomb.
    /// </summary>
    public enum MineLevel
    {
        Level77377,
        Level39,
        Level14,
        Level19,
        Level28,
    }
}
