using EggsInWild.DesertTomb.Core.Model;
using static EggsInWild.DesertTomb.Core.Enums.DesertTombEnums;

namespace EggsInWild.DesertTomb.Core.Spawning;

/// <summary>
/// Pure spawn specification for later execution (no positions / SDV types).
/// </summary>
public sealed record MonsterSpawnSpec(
    TombMonsterType Type,
    int Count,
    MonsterSpawnStats Stats,
    TombMonsterRole Role
);
