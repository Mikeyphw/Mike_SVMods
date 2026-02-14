using System.Collections.Generic;

namespace WildEggsHatching.Infrastructure.Save;

/// <summary>
/// V2 save data format with versioning and cleaner structure.
/// This is the new canonical format for save data.
/// </summary>
public class ModSaveDataV2
{
    /// <summary>The schema version for this format.</summary>
    public const int SchemaVersion = 2;

    /// <summary>Schema version for migration compatibility.</summary>
    public int Version { get; set; } = SchemaVersion;

    /// <summary>Animal IDs that have been unlocked.</summary>
    public HashSet<string> UnlockedAnimals { get; set; } = new();

    /// <summary>Animal IDs that have been discovered (hatched but not necessarily unlocked).</summary>
    public HashSet<string> DiscoveredAnimals { get; set; } = new();

    /// <summary>Note IDs that have been collected.</summary>
    public HashSet<string> CollectedNotes { get; set; } = new();

    /// <summary>Day when the player started the progression.</summary>
    public int StartDay { get; set; }

    /// <summary>Current progression step.</summary>
    public int CurrentStep { get; set; }

    /// <summary>Day when the current step was reached.</summary>
    public int CurrentStepDay { get; set; }

    /// <summary>Quest completion days tracking.</summary>
    public Dictionary<int, int> QuestCompletionDays { get; set; } = new();

    /// <summary>Weather tracking.</summary>
    public int WeatherToday { get; set; }

    /// <summary>Weather tracking.</summary>
    public int WeatherTomorrow { get; set; }

    /// <summary>Previous day's weather.</summary>
    public int WeatherYesterday { get; set; }

    /// <summary>Creates a clean V2 save data instance.</summary>
    public static ModSaveDataV2 Create()
    {
        return new ModSaveDataV2
        {
            Version = SchemaVersion,
            UnlockedAnimals = new HashSet<string>(),
            DiscoveredAnimals = new HashSet<string>(),
            CollectedNotes = new HashSet<string>(),
            StartDay = 0,
            CurrentStep = 0,
            CurrentStepDay = 0,
            QuestCompletionDays = new Dictionary<int, int>(),
            WeatherToday = 0,
            WeatherTomorrow = 0,
            WeatherYesterday = 0
        };
    }
}

