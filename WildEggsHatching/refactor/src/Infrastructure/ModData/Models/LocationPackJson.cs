using System.Collections.Generic;

namespace WildEggsHatching.Infrastructure.ModData.Models;

public sealed class LocationPackJson
{
    public string ModID { get; set; } = "";
    public int NumberOfSpawnedEggs { get; set; } = 0;
    public int MinimumSpawnsPerDay { get; set; } = 0;
    public int MaximumSpawnsPerDay { get; set; } = 0;

    /// <summary>Map of LocationType name to a list of map names.</summary>
    public Dictionary<string, List<string>> Locations { get; set; } = new();
}
