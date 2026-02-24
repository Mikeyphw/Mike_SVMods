using System.Collections.Generic;
using EggsInWild.DesertTomb.Core.Model;
using static EggsInWild.DesertTomb.Core.Enums.DesertTombEnums;

namespace EggsInWild.DesertTomb.Core.Spawning;

/// <summary>
/// Spawn plan for a single tomb area.
/// </summary>
public sealed record TombAreaSpawnPlan(
    TombAreaLayout Area,
    TombAreaType AreaType,
    IReadOnlyList<MonsterSpawnSpec> Spawns
);
