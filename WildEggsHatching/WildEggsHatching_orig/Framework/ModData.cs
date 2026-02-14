using StardewValley;
using StardewValley.Buildings;
using StardewValley.Extensions;
using WildEggsHatching.Models;
using static WildEggsHatching.EnumsAndStrings.Strings;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.Framework;

public sealed class ModData
{
    internal const string AnimalDataKey = "wildEggs-animalData";
    public HashSet<string> FarmAnimalNames { get; set; }
    public HashSet<ChosenAnimal> HatchedAnimals { get; set; }
    public HashSet<ChosenAnimal> NewAddedAnimals { get; set; }
    public Dictionary<string, HashSet<long>> AnimalsOnTheFarm { get; set; }
    public Dictionary<string, HashSet<long>> AnimalsOnTheFarmYesterday { get; set; }
    public Dictionary<string, int?> QuestCompletedDay { get; set; }
    public string WeatherYesterday { get; set; }
    public string WeatherTomorrow { get; set; }
    public string WeatherToday { get; set; }

    //  public ProgressionStep  CurrentProgressionStep { get; set; }


    public ModData()
    {
        WeatherYesterday = string.Empty;
        WeatherTomorrow = string.Empty;
        WeatherToday = string.Empty;
        HatchedAnimals = new();
        NewAddedAnimals = new();
        AnimalsOnTheFarm = new();
        FarmAnimalNames = new();
        AnimalsOnTheFarmYesterday = new();
        QuestCompletedDay = new()
        {
            { IDs.Quests.SpecialOrderWildEggsI, null },
            { IDs.Quests.WizardStudy, null },
            { IDs.Quests.SpecialOrderWildEggsII, null },
            { IDs.Quests.TheDesertScroll, null },
        };
    }

    internal void Reset()
    {
        HatchedAnimals.Clear();
        NewAddedAnimals.Clear();
        AnimalsOnTheFarm.Clear();
        FarmAnimalNames.Clear();
        AnimalsOnTheFarmYesterday.Clear();
        foreach (var quest in QuestCompletedDay.Keys)
        {
            QuestCompletedDay[quest] = null;
        }
    }

    internal void Update(HashSet<string> loadedAnimalsKeys)
    {
        LoggerHelper.VerboseLog("Update method started.");

        Reset();
        LoggerHelper.VerboseLog("Reset method called.");

        var modSaveData = ModEntry.HelperInstance.Data.ReadSaveData<ModData>(AnimalDataKey);
        if (modSaveData != null)
        {
            LoggerHelper.VerboseLog("modSaveData found, updating properties.");

            // Replace all properties with values from modSaveData
            FarmAnimalNames = new HashSet<string>(modSaveData.FarmAnimalNames);
            LoggerHelper.VerboseLog(
                $"FarmAnimalNames updated: {string.Join(", ", FarmAnimalNames)}"
            );

            HatchedAnimals = new HashSet<ChosenAnimal>(modSaveData.HatchedAnimals);
            LoggerHelper.VerboseLog($"HatchedAnimals updated: {HatchedAnimals.Count} items");

            NewAddedAnimals = new HashSet<ChosenAnimal>(modSaveData.NewAddedAnimals);
            LoggerHelper.VerboseLog($"NewAddedAnimals updated: {NewAddedAnimals.Count} items");

            // Replace AnimalsOnTheFarm with new data
            AnimalsOnTheFarm = new Dictionary<string, HashSet<long>>(modSaveData.AnimalsOnTheFarm);
            LoggerHelper.VerboseLog($"AnimalsOnTheFarm updated: {AnimalsOnTheFarm.Count} entries");

            // Replace AnimalsOnTheFarmYesterday with new data
            AnimalsOnTheFarmYesterday = new Dictionary<string, HashSet<long>>(
                modSaveData.AnimalsOnTheFarmYesterday
            );
            LoggerHelper.VerboseLog(
                $"AnimalsOnTheFarmYesterday updated: {AnimalsOnTheFarmYesterday.Count} entries"
            );

            // Replace QuestCompletedDay with new data
            QuestCompletedDay = new Dictionary<string, int?>(modSaveData.QuestCompletedDay);
            LoggerHelper.VerboseLog(
                $"QuestCompletedDay updated: {QuestCompletedDay.Count} entries"
            );
        }
        else
        {
            LoggerHelper.VerboseLog("modSaveData is null, skipping updates.");
        }

        UpdateAnimalsOnFarm(loadedAnimalsKeys, modSaveData == null);
        LoggerHelper.VerboseLog("UpdateAnimalsOnFarm method called.");

        HelperInstance.Data.WriteJsonFile("modSAVE.json", this);
        HelperInstance.Data.WriteSaveData(AnimalDataKey, this);
        LoggerHelper.VerboseLog("Save data written to modSAVE.json.");
    }

    /// <summary>
    /// Updates the current animals on the farm and preserves the state of yesterday's animals.
    /// </summary>
    /// <param name="wildAnimalKeys">The keys of the currently loaded animals.</param>
    /// <param name="isNewData">Indicates if this is new data being loaded.</param>
    internal void UpdateAnimalsOnFarm(HashSet<string> wildAnimalKeys, bool isNewData = false)
    {
        // Preserve yesterday's animals if this is not new data
        if (!isNewData)
        {
            AnimalsOnTheFarmYesterday = new(AnimalsOnTheFarm);
        }

        // Set the farm animal names and update the current animals on the farm
        SetFarmAnimalNames(wildAnimalKeys);
        SetAnimalsOnTheFarm();
    }

    private HashSet<long> GetAnimalIdsByType(string type)
    {
        var animalIds = new HashSet<long>();

        foreach (
            var animal in Game1
                .getFarm()
                .buildings.Where(b => IsCoopOrBarn(b))
                .Select(b => (AnimalHouse)b.indoors.Value)
                .SelectMany(ah => ah.animals.Values)
                .Where(a => a.type.Value.EqualsIgnoreCase(type))
        )
        {
            animalIds.Add(animal.myID.Value);
        }

        foreach (
            var animal in Game1
                .getFarm()
                .animals.Values.Where(a => a.type.Value.EqualsIgnoreCase(type))
        )
        {
            animalIds.Add(animal.myID.Value);
        }

        return animalIds;
    }

    private void SetFarmAnimalNames(HashSet<string> animals)
    {
        FarmAnimalNames = animals;
    }

    private void SetAnimalsOnTheFarm()
    {
        AnimalsOnTheFarm = FarmAnimalNames.ToDictionary(x => x, x => GetAnimalIdsByType(x));
    }

    private bool IsCoopOrBarn(Building building)
    {
        return building.buildingType.Value.ContainsIgnoreCase("coop")
            || building.buildingType.Value.ContainsIgnoreCase("barn");
    }
}
