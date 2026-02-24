using System.Collections.Generic;

namespace EggsInWild.Application.Spawning;

public sealed record SpawnPlan(
    IReadOnlyDictionary<string, int> PackEggBudgets,
    IReadOnlyList<SpawnJob> Jobs);
