using System.Collections.Generic;

namespace EggsInWild.Infrastructure.ModData.Models;

public sealed class SpawnAreaRuleJson
{
    /// <summary>Egg item-id suffix token (WEgg/LWEgg/MWEgg/LMWEgg). Used to synthesize stable item IDs from JSON fields.</summary>
    public string EggTypeToken { get; set; } = "";

    public string Location { get; set; } = "";
    public string MapName { get; set; } = "";
    public string ModID { get; set; } = "Vanilla";

    public int MinEggs { get; set; } = 0;
    public int MaxEggs { get; set; } = 0;
    public int EggsToSpawn { get; set; } = 0;

    public double RandomChance { get; set; } = 0.0;
    public bool UseBaseRandomChance { get; set; } = true;

    public List<string> IncludeTerrainTypes { get; set; } = new();
    public List<string> ExcludeTerrainTypes { get; set; } = new();
    public List<string> IncludeCoordinates { get; set; } = new();
    public List<string> ExcludeCoordinates { get; set; } = new();

    public ExtraConditionsJson ExtraConditions { get; set; } = new();

    public int SpawnedEggs { get; set; } = 0;
    public bool HitMaxSpawnTries { get; set; } = false;
}

public sealed class ExtraConditionsJson
{
    public int StartYear { get; set; } = 1;
    public List<string> Seasons { get; set; } = new();
    public List<int> Days { get; set; } = new();

    /// <summary>Optional spawn-time gating (hour 0-23). -1 disables gating.</summary>
    public int SpawnTimeHour { get; set; } = -1;

    /// <summary>Stardew 1.6 GSQ expression (or empty).</summary>
    public string GameStateQuery { get; set; } = "";

    public List<string> WeatherYesterday { get; set; } = new();
    public List<string> WeatherToday { get; set; } = new();
    public List<string> WeatherTomorrow { get; set; } = new();
}
