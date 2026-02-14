using System.Collections.Generic;
using Microsoft.Xna.Framework;
using WildEggsHatching.Core.Enums;

namespace WildEggsHatching.Application.Spawning;

/// <summary>Concrete spawn work item for today (one egg item ID in one map with area rules).</summary>
public sealed record SpawnJob(
    string EggItemId,
    EggKind EggKind,
    string PackId,
    string LocationType,
    string MapName,
    double SpawnChance,
    bool BypassRandomChance,
    int EggsToSpawn,
    IReadOnlyList<string> IncludeTerrainTypes,
    IReadOnlyList<string> ExcludeTerrainTypes,
    IReadOnlyList<Rectangle> IncludeRects,
    IReadOnlyList<Rectangle> ExcludeRects);
