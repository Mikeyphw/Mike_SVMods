using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;
using WildEggsHatching.Models;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.Managers
{
    /// <summary>The mod entry point.</summary>
    internal class WildEggManager
    {
        private readonly WildEgg Egg;

        private readonly WildListManager _wildListManager;

        internal WildEggManager(WildListManager wildListManager, WildEgg egg)
        {
            Egg = egg;
            _wildListManager = wildListManager;
            Reset();
        }

        internal void OnDayStart()
        {
            LoggerHelper.Log("Starting a new day in WildAnimalManager.");
            SelectRandomAnimals();
        }

        internal void OnDayEnding()
        {
            LoggerHelper.Log("Ending the day in WildAnimalManager.");
            Egg.ChosenAnimal = null;
            Egg.ChosenAnimalKey = null;
        }

        internal void Reset()
        {
            Egg.ChosenAnimal = null;
            Egg.ChosenAnimalKey = null;
            Egg.AnimalList.Clear();
            Egg.GameLocations.Clear();
            Egg.AnimalList.UnionWith(GetFilteredAnimals(_wildListManager.ValidWildAnimals));
            Egg.GameLocations.Clear();
        }

        private HashSet<WildAnimalData> GetFilteredAnimals(HashSet<WildAnimalData> animalList)
        {
            //LoggerHelper.Log("Starting to filter animals. Total animals in the list: " + animalList.Count);

            var filteredAnimals = animalList
                .Where(a =>
                {
                    // Log the LocationTypes for the current animal
                    // LoggerHelper.Log($"Animal: {a.Name}, LocationTypes: {string.Join(", ", a.LocationTypes.Select(kvp => $"{kvp.Key} ({kvp.Key.GetType().Name}): {kvp.Value}"))}");

                    bool isMagicMatch = a.IsMagic == Egg.IsMagic;
                    bool isLargeEggMatch = a.LargeEgg == Egg.IsLarge;
                    bool isLocationMatch =
                        a.LocationTypes.ContainsKey(Egg.Location) && a.LocationTypes[Egg.Location];

                    // LoggerHelper.Log($"Evaluating animal: {a.Name}. IsMagicMatch: {isMagicMatch}, IsLargeEggMatch: {isLargeEggMatch}, IsLocationMatch: {isLocationMatch}");

                    return isMagicMatch && isLargeEggMatch && isLocationMatch;
                })
                .ToHashSet();

            LoggerHelper.Log(
                $"Filtering complete. Total filtered animals: {filteredAnimals.Count}"
            );

            return filteredAnimals;
        }

        internal void SelectRandomAnimals()
        {
            LoggerHelper.Log("Choosing animals...");
            SelectRandomAnimal();
        }

        private void SelectRandomAnimal()
        {
            LoggerHelper.Log("Selecting a random animal...");

            if (!Egg.IsValid)
            {
                LoggerHelper.Log("Egg is not valid, skipping animal selection.");
                return;
            }

            LoggerHelper.Log("Egg is valid, proceeding with animal selection.");

            try
            {
                Egg.ChosenAnimal = Egg.AnimalList.GetRandomItem(a =>
                    ModEntry.Config.ChosenWeights[a.Name]
                );
                LoggerHelper.Log("Chosen animal: " + Egg.ChosenAnimal.Name);

                Egg.ChosenAnimalKey = Egg
                    .ChosenAnimal.FarmAnimalKeys.GetRandomItem(i => i.Weight)
                    .Id;
                LoggerHelper.Log("Chosen animal key: " + Egg.ChosenAnimalKey);
            }
            catch (Exception ex)
            {
                LoggerHelper.Log(
                    "An error occurred while selecting a random animal: " + ex.Message,
                    LogLevel.Error
                );
            }
        }

        internal string GetChosenAnimal(bool animalKey)
        {
            var animal = animalKey ? Egg.ChosenAnimalKey : Egg.ChosenAnimal?.Name;
            return animal ?? "No chosen animal";
        }
    }
}
