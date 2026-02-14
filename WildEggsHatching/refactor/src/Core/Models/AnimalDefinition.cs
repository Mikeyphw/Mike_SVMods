using System.Collections.Generic;
using WildEggsHatching.Core.Enums;

namespace WildEggsHatching.Core.Models;

/// <summary>Wild animal config loaded from JSON, after mod-availability filtering.</summary>
public sealed record WildAnimalDefinition(
    string WildAnimalKey,
    bool LargeEgg,
    int Price,
    bool IsMagic,
    IReadOnlyDictionary<LocationType, bool> LocationTypes,
    IReadOnlyList<string> FarmAnimalIdsSameWeight,
    IReadOnlyList<WeightedFarmAnimalId> FarmAnimalIdsWeighted);

public sealed record WeightedFarmAnimalId(string Id, double Weight);
