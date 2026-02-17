using System.Collections.Generic;
using WildEggsHatching.DesertTomb.Core.Model;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.DesertTomb.Core.Spawning;

/// <summary>
/// Spawn plan for a single tomb area.
/// </summary>
public sealed record TombAreaSpawnPlan(
    TombAreaLayout Area,
    TombAreaType AreaType,
    IReadOnlyList<MonsterSpawnSpec> Spawns
);
