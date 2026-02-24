using System.Collections.Generic;
using EggsInWild.Core.Models;

namespace EggsInWild.Infrastructure.ModData;

public sealed class AnimalCatalog
{
    private readonly IReadOnlyDictionary<string, WildAnimalDefinition> _animals;

    public AnimalCatalog(IReadOnlyDictionary<string, WildAnimalDefinition> animals)
    {
        _animals = animals;
    }

    public IEnumerable<WildAnimalDefinition> All() => _animals.Values;

    public bool TryGet(string wildAnimalKey, out WildAnimalDefinition definition)
        => _animals.TryGetValue(wildAnimalKey, out definition!);
}
