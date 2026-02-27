using System.Text.RegularExpressions;
using LetMeHatch.Config;
using LetMeHatch.Framework;
using Mikew.NestCore.Framework;
using LetMeHatch.Helpers;
using LetMeHatch.Integrations;
using LetMeHatch.Models;
using LetMeHatch.Infrastructure.Smapi;
using LetMeHatch.Core.Random;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Framework.Random;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using static LetMeHatch.EnumsAndStrings.Enums;
using static Mikew.NestCore.EnumsAndStrings.Strings;
using static Mikew.NestCore.EnumsAndStrings.Strings.ItemIds;
using Object = StardewValley.Object;

using static LetMeHatch.ModEntry;

namespace LetMeHatch.Breeding
{
    public class BreedingDataLoader
    {
        private readonly SavedItemsData _savedItemsData;
        private readonly CloningHelper _cloningHelper;
        private readonly Dictionary<
            string,
            Dictionary<string, BreedingDataFemale>
        > _loadedFemaleBreedingData;
        private readonly List<IContentPack> _loadedContentPacks;
        private readonly List<ClonedAnimalInfo> _clonedAnimalsInfoList;
        private readonly HashSet<string> WildEggsIDs;

        public class ContentPackConfig
        {
            public Dictionary<
                string,
                Dictionary<string, BreedingConfig>
            > BreedingConfigsByPack { get; set; }

            public ContentPackConfig()
            {
                BreedingConfigsByPack = new();
            }
        }

        public BreedingDataLoader(
            IWildEggsApi? wildEggsApi,
            Dictionary<string, Dictionary<string, BreedingDataFemale>> loadedFemaleBreedingData,
            List<IContentPack> loadedContentPacks,
            List<ClonedAnimalInfo> clonedAnimalsInfoList,
            SavedItemsData savedItemsData
        )
        {
            WildEggsIDs = wildEggsApi != null ? wildEggsApi.WildEggsIDs : new();
            _loadedFemaleBreedingData = loadedFemaleBreedingData;
            _loadedContentPacks = loadedContentPacks;
            _clonedAnimalsInfoList = clonedAnimalsInfoList;
            _savedItemsData = savedItemsData;
            _cloningHelper = new CloningHelper(_savedItemsData, new());
        }

        internal void LoadBreedingData()
        {
            _loadedContentPacks.AddRange(ModEntry.HelperInstance.ContentPacks.GetOwned());
            var roosterIntegration = CreateIntegration(
                "Mikew.roostersaddon.smapi",
                "Roosters Hatching and Control Addon",
                IntegrationName.RoosterControlAddon
            );
            var vanillaIntegration = CreateIntegration(
                "Vanilla",
                "Vanilla Game",
                IntegrationName.VanillaGame
            );
            _loadedContentPacks.Add(roosterIntegration);
            _loadedContentPacks.Add(vanillaIntegration);
            foreach (IContentPack contentPack in _loadedContentPacks)
            {
                LoggerHelper.VerboseLog(
                    $"Reading content pack: {contentPack.Manifest.Name} {contentPack.Manifest.Version} from {contentPack.DirectoryPath}"
                );
                if (!contentPack.HasFile("content.json"))
                {
                    LoggerHelper.VerboseLog($"Missing content.json file in content pack", LogLevel.Warn);
                    continue;
                }
                LoadBreedingDataFromContentPack(contentPack);
            }
            FilterOutDuplicateClones();
        }

        private IContentPack CreateIntegration(
            string contentPackID,
            string contentPackName,
            IntegrationName integrationName
        )
        {
            var integration = new Integration(integrationName);
            LoggerHelper.VerboseLog($"Creating CP for {integrationName} Integration.");
            return ModEntry.HelperInstance.ContentPacks.CreateTemporary(
                directoryPath: EnsureIntegrationDirectory(contentPackName),
                id: contentPackID,
                name: contentPackName,
                description: "...",
                author: "...",
                version: new SemanticVersion(1, 0, 0)
            );
        }

        private static string EnsureIntegrationDirectory(string contentPackName)
        {
#if MIKE_DEV
            return Path.Combine(ModEntry.HelperInstance.DirectoryPath, $"data/Integrations/{contentPackName}");
#else
            var dir = Path.Combine(ModEntry.HelperInstance.DirectoryPath, "temp", "Integrations", contentPackName);
            Directory.CreateDirectory(dir);

            // materialize embedded files for SMAPI's content-pack reader
            var contentJson = EmbeddedJson.GetTextOrThrow(
                $"data/Integrations/{contentPackName}/content.json",
                context: "LetMeHatch integration content.json"
            );
            File.WriteAllText(Path.Combine(dir, "content.json"), contentJson);

            var i18nRel = $"data/Integrations/{contentPackName}/i18n/default.json";
            if (EmbeddedTextStore.TryGet(LetMeHatch.Generated.EmbeddedTextFiles.Files, i18nRel, out var i18nJson))
            {
                var i18nDir = Path.Combine(dir, "i18n");
                Directory.CreateDirectory(i18nDir);
                File.WriteAllText(Path.Combine(i18nDir, "default.json"), i18nJson);
            }

            return dir;
#endif
        }

        private void LoadBreedingDataFromContentPack(IContentPack contentPack)
        {
            var breedingDataDic = contentPack.ReadJsonFile<Dictionary<string, BreedingDataFemale>>(
                "content.json"
            )!;

            // Filter out breeding data with invalid females
            FilterFemales(breedingDataDic, contentPack.Manifest.UniqueID);
            _loadedFemaleBreedingData[contentPack.Manifest.UniqueID] = breedingDataDic;
        }

        private void FilterFemales(
            Dictionary<string, BreedingDataFemale> femalesData,
            string contentPackID
        )
        {
            var femalesToRemove = new List<string>();

            foreach (var femaleData in femalesData)
            {
                if (Game1.farmAnimalData.ContainsKey(femaleData.Key))
                {
                    FilterMales(femaleData.Value.Males, femaleData.Key, contentPackID);
                    if (femaleData.Value.Males.Count <= 0)
                    {
                        femalesToRemove.Add(femaleData.Key);
                        LoggerHelper.VerboseLog(
                            $"Removing female '{femaleData.Key}' because it has no valid males.",
                            LogLevel.Warn
                        );
                    }
                }
                else
                {
                    femalesToRemove.Add(femaleData.Key);
                    LoggerHelper.VerboseLog(
                        $"Removing female '{femaleData.Key}' because it's not found in the game data.",
                        LogLevel.Warn
                    );
                }
            }
            foreach (var key in femalesToRemove)
            {
                femalesData.Remove(key);
            }
        }

        private void FilterMales(
            Dictionary<string, BreedingDataMale> males,
            string femaleKey,
            string contentPackID = ""
        )
        {
            var malesToRemove = new List<string>();
            var malesToAdd = new List<KeyValuePair<string, BreedingDataMale>>();
            foreach (var male in males)
            {
                // LoggerHelper.VerboseLog($"Processing {male.Key}...");
                if (Game1.farmAnimalData.ContainsKey(male.Key) || male.Key == femaleKey)
                {
                    UpdateMissingEggs(male, femaleKey);
                    if (male.Key == femaleKey)
                    {
                        // LoggerHelper.VerboseLog($"{male.Key}={femaleKey}");
                        var oldData = male.Value;
                        var newMaleKey = CloningHelper.GetClonedAnimalID(contentPackID, femaleKey);
                        var newMaleBreedingData = new BreedingDataMale()
                        {
                            AdjustableMaleChances = oldData.AdjustableMaleChances,
                            BoostByMale = oldData.BoostByMale,
                            MaleChance = oldData.MaleChance,
                            EggsIDs = new(oldData.EggsIDs),
                            Enabled = oldData.Enabled
                        };
                        newMaleBreedingData.SetEggIDs(oldData.GetEggIDs());
                        var newMale = new KeyValuePair<string, BreedingDataMale>(
                            newMaleKey,
                            newMaleBreedingData
                        );
                        malesToAdd.Add(newMale);
                        malesToRemove.Add(male.Key);
                    }
                }
                else
                {
                    malesToRemove.Add(male.Key);
                    LoggerHelper.VerboseLog(
                        $"Removing male '{male.Key}' from female '{femaleKey}' because it's not found in the game data.",
                        LogLevel.Warn
                    );
                }
            }
            foreach (var key in malesToRemove)
            {
                //  LoggerHelper.VerboseLog($"Removing {key}...");
                males.Remove(key);
            }
            foreach (var (key, value) in malesToAdd)
            {
                // LoggerHelper.VerboseLog($"Adding {key}...");
                males.Add(key, value);
            }
        }

        private void FilterOutDuplicateClones()
        {
            _clonedAnimalsInfoList.Clear();
            var femaleBredingData = _loadedFemaleBreedingData.SelectMany(x => x.Value.Values);
            var maleClones = _loadedFemaleBreedingData.SelectMany(kvp =>
                kvp.Value.Select(innerKvp =>
                    innerKvp
                        .Value.Males.Where(m => m.Key.EndsWith("Male"))
                        .Select(m => new
                        {
                            ContentPackID = kvp.Key,
                            FemaleKey = innerKvp.Key,
                            Male = m.Key
                        })
                )
            );
            var groupedMales = maleClones.SelectMany(x => x).GroupBy(x => x.FemaleKey);

            foreach (var group in groupedMales)
            {
                var femaleKey = group.Key;
                var chosenMale = group.FirstOrDefault(
                    g => g.ContentPackID == "Vanilla",
                    group.First()
                );
                var cloneInfo = _loadedFemaleBreedingData[chosenMale.ContentPackID][
                    chosenMale.FemaleKey
                ].CloneConfig;
                cloneInfo.ContentPackID = chosenMale.ContentPackID;
                cloneInfo.FemaleKey = chosenMale.FemaleKey;
                cloneInfo.MaleKey = chosenMale.Male;
                LoggerHelper.VerboseLog(
                    $"Found cloned male from {chosenMale.Male} from content pack {chosenMale.ContentPackID}"
                );
                if (!_clonedAnimalsInfoList.Contains(cloneInfo))
                {
                    _clonedAnimalsInfoList.Add(cloneInfo);
                }

                // Remove duplicates
                foreach (var male in group)
                {
                    if (cloneInfo.MaleKey.Equals(male.Male))
                    {
                        continue;
                    }
                    // Remove the duplicate male from the data
                    foreach (var fd in _loadedFemaleBreedingData.Values)
                    {
                        foreach (var bd in fd.Values.ToList())
                        {
                            if (bd.Males.ContainsKey(male.Male))
                            {
                                bd.Males.Remove(male.Male);
                                LoggerHelper.VerboseLog($"Removing duplicate '{male.Male}'.", LogLevel.Warn);
                            }
                        }
                    }
                }
            }

            FilterOutEmptyFemaleBreedingData(_loadedFemaleBreedingData);
        }

        private void FilterOutEmptyFemaleBreedingData(
            Dictionary<string, Dictionary<string, BreedingDataFemale>> loadedBreedingData
        )
        {
            // Create a copy of the keys to avoid modifying the collection while iterating
            var keysToRemove = loadedBreedingData
                .Where(x => !x.Value.Any(y => y.Value.Males.Any()))
                .Select(x => x.Key)
                .ToList();

            // Remove the items that don't have any males
            foreach (var key in keysToRemove)
            {
                loadedBreedingData.Remove(key);
                LoggerHelper.VerboseLog(
                        $"Removing female '{key}' because it has no males.",
                        LogLevel.Warn
                    );
            }

            // Filter out the inner items that don't have any males
            foreach (var kvp in loadedBreedingData.ToList())
            {
                var innerKeysToRemove = kvp
                    .Value.Where(y => !y.Value.Males.Any())
                    .Select(y => y.Key)
                    .ToList();

                foreach (var innerKey in innerKeysToRemove)
                {
                    kvp.Value.Remove(innerKey);
                    LoggerHelper.VerboseLog(
                        $"Removing '{innerKey}' because it has no males.",
                        LogLevel.Warn
                    );
                }
            }
        }

        private void UpdateMissingEggs(
            KeyValuePair<string, BreedingDataMale> male,
            string femaleKey
        )
        {
            var eggs = male.Value.EggsIDs;
            var foundEggs = FindEgg(femaleKey, Game1.farmAnimalData[femaleKey]);
            foreach (var eggType in eggs.Keys.Where(e => e != ItemType.FertilizedEgg))
            {
                var foundEgg = eggs[eggType].IsEmptyOrWhitespace()
                    ? foundEggs[eggType]
                    : eggs[eggType];
                male.Value.SetEggID(eggType, foundEgg);
            }
            var finalEggs = male.Value.GetEggIDs();
            var originalEgg = finalEggs[ItemType.Large] ?? finalEggs[ItemType.Normal];
            originalEgg ??= AnimalProducts.WhiteEgg;
            bool isFertilizedEggEmpty = eggs[ItemType.FertilizedEgg].IsEmptyOrWhitespace();
            string? fertilizedEggID;

            if (isFertilizedEggEmpty)
            {
                if (ShouldUseMearasFertilizedEggs(male.Key, femaleKey))
                {
                    fertilizedEggID = GetMearaFertilizedEggID(femaleKey, male.Key);
                }
                else
                {
                    // LoggerHelper.VerboseLog(
                    //     $"Found Eggs for {femaleKey}:\nNormal: {finalEggs[ItemOrEggType.Normal]}\nLarge: {finalEggs[ItemOrEggType.Large]}"
                    // );
                    fertilizedEggID = _cloningHelper.GetFertilizedClonedEggID(
                        femaleKey,
                        originalEgg
                    );
                }
            }
            else
            {
                fertilizedEggID = eggs[ItemType.FertilizedEgg];
            }
            var fertilizedEgg = fertilizedEggID;
            male.Value.SetEggID(ItemType.FertilizedEgg, fertilizedEgg);
        }

        public static bool ShouldUseMearasFertilizedEggs(string roosterKey, string femaleKey)
        {
            var hasMearaRoostersMod = ModIdEvaluator.Evaluate("Meara.Roosters", HelperInstance.ModRegistry);

            var malesForMearaMod = new string[]
            {
                "cockwhite",
                "cockbrown",
                "cockblue",
                "cockvoid",
                "cockgold"
            };

            bool isMaleForMearaMod = malesForMearaMod.Contains(roosterKey);
            bool result = hasMearaRoostersMod && isMaleForMearaMod;
            return result;
        }

        private string GetMearaFertilizedEggID(string femaleKey, string maleKey)
        {
            var chickenTypes = new Dictionary<string, string>
            {
                { VanillaAnimals.WhiteChicken, CustomItems.FertilizedWhiteEgg },
                { VanillaAnimals.BrownChicken, CustomItems.FertilizedBrownEgg },
                { VanillaAnimals.BlueChicken, CustomItems.FertilizedBlueEgg },
                { VanillaAnimals.VoidChicken, CustomItems.FertilizedVoidEgg },
                { VanillaAnimals.GoldenChicken, CustomItems.FertilizedGoldEgg },
            };

            if (chickenTypes.TryGetValue(femaleKey, out var fertilizedEggId))
            {
                return fertilizedEggId;
            }

            var cockTypes = new Dictionary<string, string>
            {
                { "cockwhite", CustomItems.FertilizedWhiteEgg },
                { "cockbrown", CustomItems.FertilizedBrownEgg },
                { "cockblue", CustomItems.FertilizedBlueEgg },
                { "cockgold", CustomItems.FertilizedVoidEgg },
                { "cockvoid", CustomItems.FertilizedGoldEgg },
            };

            if (cockTypes.TryGetValue(maleKey, out fertilizedEggId))
            {
                return fertilizedEggId;
            }

            return "";
        }

        private Dictionary<ItemType, string?> FindEgg(string femaleKey, FarmAnimalData animalData)
        {
            var foundEggs = GetVanillaEggsID(femaleKey);
            if (foundEggs.Any(e => e.Value == null))
            {
                if (
                    _savedItemsData.AnimalItemRelation.ContainsKey(femaleKey)
                    && _savedItemsData.AnimalItemRelation[femaleKey][CloneItemType.Egg] != null
                )
                {
                    ProcessModData(femaleKey, foundEggs);
                }
                else
                {
                    ProcessGameData(animalData, foundEggs);
                }
                if (
                    string.IsNullOrEmpty(foundEggs[ItemType.Normal])
                    && string.IsNullOrEmpty(foundEggs[ItemType.Large])
                )
                {
                    foundEggs[ItemType.Normal] = AnimalProducts.WhiteEgg;
                    _savedItemsData.AnimalItemRelation.TryAdd(femaleKey, new());
                    _savedItemsData.AnimalItemRelation[femaleKey].TryAdd(CloneItemType.Egg, new());
                    _savedItemsData.AnimalItemRelation[femaleKey][CloneItemType.Egg] ??= new();
                    _savedItemsData.AnimalItemRelation[femaleKey][CloneItemType.Egg]![
                        ItemType.Normal
                    ] = AnimalProducts.WhiteEgg;
                    _savedItemsData.AnimalItemRelation[femaleKey][CloneItemType.Egg]![
                        ItemType.Large
                    ] = string.Empty;
                }
            }
            return foundEggs;
        }

        private void ProcessModData(string femaleKey, Dictionary<ItemType, string?> foundEggs)
        {
            var animalEggRelation = _savedItemsData.AnimalItemRelation[femaleKey][
                CloneItemType.Egg
            ]!;
            var normalEggId = animalEggRelation[ItemType.Normal];
            var largeEggId = animalEggRelation[ItemType.Large];
            foundEggs[ItemType.Normal] = !string.IsNullOrEmpty(normalEggId)
                ? _cloningHelper.GetClonedItemID(
                    femaleKey,
                    normalEggId,
                    CloneItemType.Egg,
                    ItemType.Normal
                )
                : null;
            foundEggs[ItemType.Large] = !string.IsNullOrEmpty(largeEggId)
                ? _cloningHelper.GetClonedItemID(
                    femaleKey,
                    largeEggId,
                    CloneItemType.Egg,
                    ItemType.Large
                )
                : null;
        }

        private void ProcessGameData(
            FarmAnimalData animalData,
            Dictionary<ItemType, string?> foundEggs
        )
        {
            var eggsList = animalData.EggItemIds;
            var produceItemIds = animalData.ProduceItemIds.Union(animalData.DeluxeProduceItemIds);
            var allEggIds = GetEggIdsFromGameData(eggsList, produceItemIds);
            var largeEggs = allEggIds.Where(e => IsLargeEgg(e)).ToList();
            var normalEggs = allEggIds.Except(largeEggs).ToList();
            foundEggs[ItemType.Normal] ??= normalEggs.FirstOrDefault();
            foundEggs[ItemType.Large] ??= largeEggs.FirstOrDefault();
        }

        private List<string> GetEggIdsFromGameData(
            List<string> eggsList,
            IEnumerable<FarmAnimalProduce> produceItemIds
        )
        {
            var eggIds = new List<string>();
            if (eggsList != null)
            {
                eggIds.AddRange(eggsList.Where(e => IsEgg(e)));
            }
            eggIds.AddRange(produceItemIds.Select(p => p.ItemId).Where(p => IsEgg(p)));
            return eggIds.Distinct().ToList();
        }

        private bool IsEgg(string itemId)
        {
            return Game1.objectData[itemId].Category == Object.EggCategory
                || Regex.IsMatch(itemId, @"\b(egg)\b", RegexOptions.IgnoreCase);
        }

        private bool IsLargeEgg(string eggId)
        {
            var VanillaLargeEggs = new string[]
            {
                AnimalProducts.LargeBrownEgg,
                AnimalProducts.LargeWhiteEgg
            };
            return Regex.IsMatch(eggId, @"(large|giant|big)", RegexOptions.IgnoreCase)
                || (
                    CloningHelper.IsClonedEgg(eggId)
                    && VanillaLargeEggs.Contains(
                        _cloningHelper.GetOriginalItemAndAnimalID(eggId).OriginalItemID
                    )
                );
        }

        private Dictionary<ItemType, string?> GetVanillaEggsID(string animalKey)
        {
            (string? NormalEgg, string? NormalLargeEgg) eggs = (null, null);
            if (VanillaAnimals.AnimalKeys.Contains(animalKey))
            {
                eggs = animalKey switch
                {
                    VanillaAnimals.WhiteChicken
                        => (AnimalProducts.WhiteEgg, AnimalProducts.LargeWhiteEgg),
                    VanillaAnimals.BrownChicken
                        => (AnimalProducts.BrownEgg, AnimalProducts.LargeBrownEgg),
                    VanillaAnimals.VoidChicken => (AnimalProducts.VoidEgg, null),
                    VanillaAnimals.GoldenChicken => (AnimalProducts.GoldenEgg, null),
                    VanillaAnimals.Duck => (AnimalProducts.DuckEgg, null),
                    VanillaAnimals.Ostrich => (AnimalProducts.OstrichEgg, null),
                    VanillaAnimals.Dinosaur => (AnimalProducts.DinoEgg, null),
                    _ => eggs
                };
            }
            return new()
            {
                [ItemType.Normal] = eggs.NormalEgg,
                [ItemType.Large] = eggs.NormalLargeEgg,
            };
        }

        public void RunHatchingSystem(
            IDictionary<string, FarmAnimalData> farmAnimalsData,
            ModConfig config,
            Dictionary<string, Dictionary<string, BreedingConfig>> breedingConfigsByPack
        )
        {
            var validBreedingData = GetBreedingDataGroupedByFemale();
            var sharedRng = config.DeterministicBreedingRng ? null : new GameRandomProvider();
            foreach (var (femaleKey, breedingDataList) in validBreedingData)
            {
                IRandomProvider rng;
                if (config.DeterministicBreedingRng)
                {
                    var seed = DeterministicSeed.For(
                        Game1.uniqueIDForThisGame,
                        Game1.Date.Year,
                        Game1.Date.Season,
                        Game1.Date.DayOfMonth,
                        femaleKey,
                        "hatching"
                    );
                    rng = new SeededRandomProvider(seed);
                }
                else
                {
                    rng = sharedRng!;
                }

                foreach (var male in breedingDataList.SelectMany(x => x.BreedingData.Males))
                {
                    male.Value.SetQuantity(male.Key);
                }
                var chosenFemaleData = breedingDataList
                    .Where(x => x.BreedingData.Males.Any())
                    .GetRandomItem(i => 1, randomProvider: rng);
                var chosenMaleData = chosenFemaleData
                    .BreedingData.Males.Where(x => x.Value.GetQuantity() > 0)
                    .GetRandomItem(m => m.Value.GetQuantity(), randomProvider: rng);
                if (chosenFemaleData.BreedingData == null)
                {
                    LoggerHelper.VerboseLog(
                        $"Skipped female {femaleKey} because did not find a breeding data."
                    );
                    continue;
                }
                if (chosenMaleData.Key == null)
                {
                    LoggerHelper.VerboseLog(
                        $"Skipped female {femaleKey} because did not find a male breeding data."
                    );
                    continue;
                }
                var currentBreedingSystem = new BreedingSystem(
                    WildEggsIDs,
                    breedingConfigsByPack,
                    config.OnlyFertilizedEggs,
                    femaleKey,
                    chosenFemaleData,
                    chosenMaleData,
                    farmAnimalsData,
                    rng
                );
                currentBreedingSystem.Run();
            }
        }

        private Dictionary<
            string,
            IEnumerable<(string ContentPackID, BreedingDataFemale BreedingData)>
        > GetBreedingDataGroupedByFemale()
        {
            return _loadedFemaleBreedingData
                .SelectMany(x => x.Value.Select(y => (x.Key, BreedingData: y)))
                .GroupBy(x => x.BreedingData.Key)
                .ToDictionary(x => x.Key, x => x.Select(y => (y.Key, y.BreedingData.Value)));
        }
    }
}