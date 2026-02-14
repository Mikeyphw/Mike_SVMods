using System.Collections.Generic;

namespace WildEggsHatching.Infrastructure.Save;

/// <summary>
/// V1 save data format (legacy compatibility).
/// This maps to the original ModData structure used by the mod.
/// </summary>
public class ModSaveDataV1
{
    /// <summary>The save data key used by the mod.</summary>
    public const string SaveKey = "wildEggs-animalData";

    /// <summary>Animals that have hatched (legacy format).</summary>
    public List<ChosenAnimalData> HatchedAnimals { get; set; } = new();

    /// <summary>Notes that have been collected.</summary>
    public HashSet<string> CollectedNotes { get; set; } = new();

    /// <summary>Quest completion days.</summary>
    public Dictionary<string, int> QuestCompletedDay { get; set; } = new();

    /// <summary>Weather tracking.</summary>
    public int WeatherToday { get; set; }

    /// <summary>Weather tracking.</summary>
    public int WeatherTomorrow { get; set; }

    /// <summary>Previous day's weather.</summary>
    public int WeatherYesterday { get; set; }

    /// <summary>Legacy format for individual hatched animal data.</summary>
    public class ChosenAnimalData
    {
        /// <summary>The farm animal name.</summary>
        public string? FarmAnimalName { get; set; }

        /// <summary>The animal type (e.g., "Chicken", "Duck").</summary>
        public string? AnimalType { get; set; }

        /// <summary>Whether this was from a large egg.</summary>
        public bool IsLargeEgg { get; set; }

        /// <summary>Whether this was from a magic egg.</summary>
        public bool IsMagicEgg { get; set; }
    }
}

