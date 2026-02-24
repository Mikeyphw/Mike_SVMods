using System.Collections.Generic;

namespace EggsInWild.Infrastructure.ModData.Models;

public sealed class ExtraModAnimalKeysJson
{
    public List<string> FarmAnimalKeysSameWeight { get; set; } = new();
    public List<WeightedIdJson> FarmAnimalKeysWeighted { get; set; } = new();
}
