using WildEggsHatching.DesertTomb.Core.Model;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.DesertTomb.Core.Spawning;

/// <summary>
/// Pure spawn specification for later execution (no positions / SDV types).
/// </summary>
public sealed record MonsterSpawnSpec(
    TombMonsterType Type,
    int Count,
    MonsterSpawnStats Stats,
    MonsterSpawnRole Role
);
