using System.Collections.Generic;

namespace WildEggsHatching.Application.Spawning;

public sealed record SpawnPlan(
    IReadOnlyDictionary<string, int> PackEggBudgets,
    IReadOnlyList<SpawnJob> Jobs);
