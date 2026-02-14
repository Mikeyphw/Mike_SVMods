using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using WildEggsHatching.EnumsAndStrings;
using WildEggsHatching.Models;
using static Mikew.BirdsOfAFeather.Helpers.ModAssetHandler;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.Managers
{
    /// <summary>The mod entry point.</summary>
    ///
    internal class WildListManager
    {
        private readonly ILetMeHatchApi? _letMeHatchApi;
        internal HashSet<WildAnimalData> WildAnimals { get; set; }
        internal HashSet<string> LoadedAnimalsKeys { get; set; }
        internal HashSet<WildAnimalData> ValidWildAnimals { get; set; }

        internal WildListManager(ILetMeHatchApi? letMeHatchApi)
        {
            ValidWildAnimals = new();
            WildAnimals = new();
            LoadedAnimalsKeys = new();
            _letMeHatchApi = letMeHatchApi;
        }

        internal void Reset()
        {
            LoggerHelper.VerboseLog("Resetting WildAnimalManager state.");
            ValidWildAnimals.Clear();
            WildAnimals.Clear();
            LoadedAnimalsKeys.Clear();
            LoggerHelper.VerboseLog("State reset complete.");
        }

        internal void OnDayStart()
        {
            Reset();
            SetUpAnimalLists();
        }

        internal void SetUpAnimalLists()
        {
            LoggerHelper.Log("Setting up animal lists.");
            var wildAnimalsInputList =
                ModEntry.HelperInstance.Data.ReadJsonFile<Dictionary<string, InputWildAnimalData>>(
                    "ModData/WildAnimalsList.json"
                ) ?? new();
            ModEntry.HelperInstance.Data.WriteJsonFile(
                "ModData/readList.json",
                wildAnimalsInputList
            );

            WildAnimals = wildAnimalsInputList
                .Select(a => a.Value.GetWildAnimalData(a.Key))
                .ToHashSet();
            ModEntry.HelperInstance.Data.WriteJsonFile("ModData/FullList.json", WildAnimals);

            LoggerHelper.Log($"Loaded {WildAnimals.Count} wild animals from the input list.");

            ValidWildAnimals = FilterOutNotLoadedMods();
            ModEntry.HelperInstance.Data.WriteJsonFile("ModData/valid.json", ValidWildAnimals);

            FilterOutInvalidAnimalKeys();
            LoadedAnimalsKeys = GetLoadedAnimalsKeys();
            WildEggsApi.Instance.SetValidAnimals(LoadedAnimalsKeys);

            LoggerHelper.VerboseLog("Animal lists set up complete.");
        }

        private HashSet<WildAnimalData> FilterOutNotLoadedMods()
        {
            LoggerHelper.Log("Filtering out not loaded mods.");
            var validWildAnimals = WildAnimals
                .Where(animalEntry => ModIdEvaluator.Evaluate(animalEntry.ModID))
                .ToHashSet();

            LoggerHelper.Log($"Found {validWildAnimals.Count} valid wild animals after filtering.");

            foreach (var wildAnimal in validWildAnimals)
            {
                wildAnimal.FarmAnimalKeys.Clear();
                foreach (
                    var (modId, animalKeys) in wildAnimal.ExtraModAnimalKeys.Where(x =>
                        ModIdEvaluator.Evaluate(x.Key)
                    )
                )
                {
                    var keysWithSameWeight = animalKeys.FarmAnimalKeysSameWeight.Select(
                        k => new AnimalKeyData(k)
                    );
                    wildAnimal.FarmAnimalKeys.AddRange(keysWithSameWeight);
                    wildAnimal.FarmAnimalKeys.AddRange(animalKeys.FarmAnimalKeysWeighted);

                    // Log skipped animal keys
                    var skippedAnimalKeys = animalKeys.FarmAnimalKeysSameWeight.Where(k =>
                        !ModIdEvaluator.Evaluate(modId)
                    );
                    foreach (var skippedKey in skippedAnimalKeys)
                    {
                        LoggerHelper.VerboseLog(
                            $"Skipped animal key {skippedKey} because mod {modId} is not installed."
                        );
                    }
                }
            }

            // Log skipped animals
            var skippedAnimals = WildAnimals
                .Where(animalEntry => !ModIdEvaluator.Evaluate(animalEntry.ModID))
                .Select(animalEntry =>
                    $"{animalEntry.Name} because {animalEntry.ModID} is not installed."
                );

            foreach (var skippedAnimal in skippedAnimals)
            {
                LoggerHelper.VerboseLog($"Skipped {skippedAnimal}");
            }

            if (_letMeHatchApi != null)
            {
                ProcessClonedAnimalKeys();
            }

            LoggerHelper.VerboseLog("Filtering out not loaded mods complete.");
            return validWildAnimals;
        }

        public void ProcessClonedAnimalKeys()
        {
            LoggerHelper.VerboseLog("Starting to process animal keys.");

            // Get the list of female keys
            var clonedAnimalsInfo = _letMeHatchApi!.GetClonedAnimalsInfo();
            var females = new List<string>(); // Assuming FemaleKey is of type int

            foreach (var (FemaleKey, MaleKey, AddAsAlternatePurchase) in clonedAnimalsInfo)
            {
                females.Add(FemaleKey);
                LoggerHelper.VerboseLog($"Added FemaleKey: {FemaleKey}");
            }

            // Iterate through each farm animal keys
            foreach (var farmAnimalKeys in ValidWildAnimals.Select(a => a.FarmAnimalKeys))
            {
                LoggerHelper.VerboseLog($"Processing farm animal keys: {farmAnimalKeys}");

                var keysToAdd = new List<AnimalKeyData>(); // List to hold new AnimalKeyData objects

                // Use Where to filter keys that are in females
                var matchingKeys = farmAnimalKeys.Where(key => females.Contains(key.Id));

                foreach (var key in matchingKeys.Select(k => k.Id))
                {
                    LoggerHelper.VerboseLog($"Found matching key: {key}");

                    // Find the corresponding male key
                    foreach (var (FemaleKey, MaleKey, AddAsAlternatePurchase) in clonedAnimalsInfo)
                    {
                        if (FemaleKey == key)
                        {
                            keysToAdd.Add(new AnimalKeyData(MaleKey));
                            LoggerHelper.VerboseLog(
                                $"Matched FemaleKey: {FemaleKey} with MaleKey: {MaleKey}"
                            );
                            break; // Exit the loop once we find the first match
                        }
                    }
                }

                // Add the new animal keys to the farmAnimalKeys
                farmAnimalKeys.AddRange(keysToAdd);
                LoggerHelper.VerboseLog($"Added {keysToAdd.Count} new keys to farm animal keys.");
            }

            LoggerHelper.VerboseLog("Finished processing animal keys.");
        }

        private HashSet<string> GetLoadedAnimalsKeys()
        {
            LoggerHelper.VerboseLog("Getting loaded animal keys.");
            var allKeys = ValidWildAnimals
                .SelectMany(a => a.FarmAnimalKeys.Select(a => a.Id))
                .ToHashSet();

            var loadedKeys = allKeys.GroupBy(x => x).Select(g => g.First()).ToHashSet();

            // Log the repeating keys with their modID and animal name
            var repeatingKeys = allKeys.GroupBy(x => x).Where(g => g.Count() > 1).ToHashSet();

            foreach (var repeatingKey in repeatingKeys.Select(k => k.Key))
            {
                var associatedAnimals = ValidWildAnimals
                    .Where(a => a.FarmAnimalKeys.Any(k => k.Id == repeatingKey))
                    .ToList();

                // Choose the first animal that has the repeating key
                var chosenAnimal = associatedAnimals[0];

                LoggerHelper.Log(
                    $"Chosen key: {repeatingKey} (ModID: {chosenAnimal.ModID}, Animal Name: {chosenAnimal.Name})",
                    LogLevel.Warn
                );

                foreach (var associatedAnimal in associatedAnimals)
                {
                    if (associatedAnimal != chosenAnimal)
                    {
                        // Remove the repeating key from the other animals
                        associatedAnimal.FarmAnimalKeys = associatedAnimal
                            .FarmAnimalKeys.Where(k => k.Id != repeatingKey)
                            .ToHashSet();

                        // If FarmAnimalKeys becomes empty, remove the entry from ValidWildAnimals
                        if (associatedAnimal.FarmAnimalKeys.Count == 0)
                        {
                            ValidWildAnimals.Remove(associatedAnimal);
                            LoggerHelper.Log(
                                $"Removed animal {associatedAnimal.Name} (ModID: {associatedAnimal.ModID}) because its FarmAnimalKeys became empty after removing duplicate key {repeatingKey}.",
                                LogLevel.Warn
                            );
                        }
                        else
                        {
                            LoggerHelper.Log(
                                $"Removed duplicate key {repeatingKey} from animal {associatedAnimal.Name} (ModID: {associatedAnimal.ModID}).",
                                LogLevel.Warn
                            );
                        }
                    }
                }
            }

            LoggerHelper.VerboseLog("Loaded animal keys obtained.");
            return loadedKeys;
        }

        private void FilterOutInvalidAnimalKeys()
        {
            LoggerHelper.VerboseLog("Filtering out invalid animal keys.");
            var updated = false;

            foreach (var animalKeys in ValidWildAnimals.Select(a => a.FarmAnimalKeys))
            {
                var skippedKeys = animalKeys
                    .Where(key =>
                        !Game1.farmAnimalData.ContainsKey(key.Id) && !IsClonedAnimal(key.Id)
                    )
                    .ToList();

                foreach (var skippedKey in skippedKeys.Select(k => k.Id))
                {
                    // Removed similarity check, now only log if the key is not found
                    if (
                        !Game1.farmAnimalData.ContainsKey(skippedKey) && !IsClonedAnimal(skippedKey)
                    )
                    {
                        LoggerHelper.Log(
                            $"Skipped {skippedKey} because it is not enabled by its mod or the key is wrong.",
                            LogLevel.Warn
                        );
                    }
                }

                animalKeys.RemoveWhere(key =>
                    !Game1.farmAnimalData.ContainsKey(key.Id) && !IsClonedAnimal(key.Id)
                );

                updated |= skippedKeys.Any();
            }

            if (updated)
            {
                LoadedAnimalsKeys = GetLoadedAnimalsKeys();
                WildEggsApi.Instance.SetValidAnimals(LoadedAnimalsKeys);
            }
            LoggerHelper.VerboseLog("Invalid animal keys filtered out.");
        }

        private bool IsClonedAnimal(string animalKey)
        {
            LoggerHelper.VerboseLog($"Checking if {animalKey} is a cloned animal.");
            bool result =
                _letMeHatchApi != null
                && _letMeHatchApi.GetClonedAnimalsInfo().Exists(a => a.MaleKey == animalKey);
            LoggerHelper.VerboseLog($"IsClonedAnimal result for {animalKey}: {result}");
            return result;
        }

        private string GetAnimalTexturePath(string animal, string modsLoaded)
        {
            LoggerHelper.VerboseLog(
                $"Getting texture path for animal: {animal} with mods loaded: {modsLoaded}."
            );
            var animalTexture = AssetHandler.GetAssetPath(animal, AssetPathType.ModFolderPath);
            return animalTexture.Replace(".png", $"-{modsLoaded}.png");
        }

        internal HashSet<WildAnimalData> GetValidAnimals(
            string building,
            bool isMagic,
            bool moddedAnimals
        )
        {
            var isLarge = building == "Barn";
            if (moddedAnimals)
            {
                return ValidWildAnimals
                    .Where(a =>
                        a.IsMagic == isMagic
                        && a.LargeEgg == isLarge
                        && !a.ModID.EqualsIgnoreCase(Strings.Vanilla)
                    )
                    .ToHashSet();
            }
            else
            {
                return ValidWildAnimals
                    .Where(a =>
                        a.IsMagic == isMagic
                        && a.LargeEgg == isLarge
                        && a.ModID.EqualsIgnoreCase(Strings.Vanilla)
                    )
                    .ToHashSet();
            }
        }
    }
}
