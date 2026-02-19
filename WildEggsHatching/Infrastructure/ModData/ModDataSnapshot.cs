using System;
using System.Collections.Generic;
using WildEggsHatching.Infrastructure.ModData.Models;

namespace WildEggsHatching.Infrastructure.ModData;

public sealed record ModDataSnapshot(
    IReadOnlyDictionary<string, WildAnimalJson> WildAnimals,
    IReadOnlyDictionary<string, List<SpawnAreaRuleJson>> SpawnAreas,
    SpawnParametersJson SpawnParameters,
    IReadOnlyDictionary<string, LocationPackJson> LocationPacks)
{
    public static ModDataSnapshot Empty { get; } = new(
        WildAnimals: new Dictionary<string, WildAnimalJson>(StringComparer.OrdinalIgnoreCase),
        SpawnAreas: new Dictionary<string, List<SpawnAreaRuleJson>>(StringComparer.OrdinalIgnoreCase),
        SpawnParameters: new SpawnParametersJson(),
        LocationPacks: new Dictionary<string, LocationPackJson>(StringComparer.OrdinalIgnoreCase));
}
