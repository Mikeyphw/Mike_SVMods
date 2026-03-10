using System;
using System.Collections.Generic;
using EggsInWild.Infrastructure.ModData.Models;

namespace EggsInWild.Infrastructure.ModData;

public sealed record ModDataSnapshot(
    IReadOnlyDictionary<string, WildAnimalJson> WildAnimals,
    IReadOnlyDictionary<string, WildEggSpawnLocationJson> WildEggSpawnDefinitions,
    SpawnParametersJson SpawnParameters,
    IReadOnlyDictionary<string, LocationPackJson> LocationPacks)
{
    public static ModDataSnapshot Empty { get; } = new(
        WildAnimals: new Dictionary<string, WildAnimalJson>(StringComparer.OrdinalIgnoreCase),
        WildEggSpawnDefinitions: new Dictionary<string, WildEggSpawnLocationJson>(StringComparer.OrdinalIgnoreCase),
        SpawnParameters: new SpawnParametersJson(),
        LocationPacks: new Dictionary<string, LocationPackJson>(StringComparer.OrdinalIgnoreCase));
}
