using System.Collections.Generic;

namespace WildEggsHatching.Infrastructure.ModData.Models;

public sealed class WildAnimalJson
{
    public bool LargeEgg { get; set; } = false;
    public int Price { get; set; } = 0;
    public bool IsMagic { get; set; } = false;

    /// <summary>Map of LocationType name to allowed/not.</summary>
    public Dictionary<string, bool> LocationTypes { get; set; } = new();

    /// <summary>Map of mod unique ID to the farm animal IDs that mod provides.</summary>
    public Dictionary<string, ExtraModAnimalKeysJson> ExtraModAnimalKeys { get; set; } = new();
}
