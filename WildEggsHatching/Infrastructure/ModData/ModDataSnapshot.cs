using System.Collections.Generic;
using WildEggsHatching.Infrastructure.ModData.Models;

namespace WildEggsHatching.Infrastructure.ModData;

public sealed record ModDataSnapshot(
    IReadOnlyDictionary<string, WildAnimalJson> WildAnimals,
    IReadOnlyDictionary<string, List<SpawnAreaRuleJson>> SpawnAreas,
    SpawnParametersJson SpawnParameters,
    IReadOnlyDictionary<string, LocationPackJson> LocationPacks);
