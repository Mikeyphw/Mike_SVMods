using LetMeHatchThisEgg.Config;
using LetMeHatchThisEgg.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings.ItemIds;
using Object = StardewValley.Object;

using static LetMeHatchThisEgg.ModEntry; namespace LetMeHatchThisEgg.Cloning
{
    public class AnimalItemRelationsManager
    {
        public Dictionary<CloneItemType, List<string>> AnimalVanillaItemRelationOrder
        {
            get;
            private set;
        } =
            new()
            {
                { CloneItemType.Egg, new() },
                { CloneItemType.Milk, new() },
                { CloneItemType.Misc, new() },
            };
        public Dictionary<
            string,
            Dictionary<CloneItemType, Dictionary<ItemType, string>>
        > DefaultValues { get; set; }
        private readonly SavedItemsData SavedItemsData;

        public AnimalItemRelationsManager(
            SavedItemsData savedItemsData
        )
        {
            SavedItemsData = savedItemsData;
            DefaultValues = new();
        }

        public void HandleAnimalEggRelations()
        {
            var animalDataDic = Game1.farmAnimalData;
            Dictionary<CloneItemType, List<string>> animalsWithVanillaItems = new();
            Dictionary<CloneItemType, List<string>> animalsWithoutVanillaItems = new();
            Dictionary<CloneItemType, List<string>> animalsWithRepeatingVanillaItem = new();
            foreach (var animalKey in SavedItemsData.AnimalItemRelation.Keys)
            {
                UpdateEggRelations(animalKey);
            }

            foreach (var cloneItemType in GetCloneItemTypes())
            {
                var animalProducts = GetAnimalProductsPerCloneItemTypes(cloneItemType);
                var category = GetCategoryPerCloneItemTypes(cloneItemType);

                animalsWithVanillaItems[cloneItemType] = animalDataDic
                    .Where(a =>
                        !VanillaAnimals.AnimalKeys.Contains(a.Key)
                        && CheckIfProduceVanillaItem(a.Value, animalProducts)
                    )
                    .Select(x => x.Key)
                    .ToList();
                if (ModEntry.Config.ExclusiveRabbitWool && cloneItemType == CloneItemType.Misc)
                {
                    animalsWithVanillaItems[cloneItemType].Add(VanillaAnimals.Rabbit);
                }

                animalsWithoutVanillaItems[cloneItemType] =
                    cloneItemType != CloneItemType.Egg
                        ? new()
                        : animalDataDic
                            .Where(a =>
                                !CheckIfProduceAnyItemOfThisType(a.Value, animalProducts, category)
                            )
                            .Select(x => x.Key)
                            .ToList();
                animalsWithRepeatingVanillaItem[cloneItemType] = GetAnimalsWithRepeatingItems(
                    animalDataDic,
                    category,
                    animalProducts,
                    cloneItemType
                );

                var finalList = animalsWithVanillaItems[cloneItemType]
                    .Union(animalsWithRepeatingVanillaItem[cloneItemType])
                    .Union(animalsWithoutVanillaItems[cloneItemType])
                    .Distinct();
                AnimalVanillaItemRelationOrder[cloneItemType] = finalList.ToList();

                ModEntry.HelperInstance?.Data.WriteJsonFile(
                    $"lists/animalsWith{cloneItemType}.json",
                    animalsWithVanillaItems[cloneItemType]
                );
                ModEntry.HelperInstance?.Data.WriteJsonFile(
                    $"lists/animalsWithout{cloneItemType}.json",
                    animalsWithoutVanillaItems[cloneItemType]
                );
                ModEntry.HelperInstance?.Data.WriteJsonFile(
                    $"lists/animalsWithRepeating{cloneItemType}.json",
                    animalsWithRepeatingVanillaItem[cloneItemType]
                );

                ModEntry.HelperInstance?.Data.WriteJsonFile(
                    $"lists/final{cloneItemType}List.json",
                    finalList
                );
                var lists = new List<string>[]
                {
                    animalsWithVanillaItems[cloneItemType],
                    animalsWithRepeatingVanillaItem[cloneItemType],
                    animalsWithoutVanillaItems[cloneItemType]
                };

                var animalsWithoutEggRelations = animalDataDic.Keys.Where(k =>
                    !SavedItemsData.AnimalItemRelation.ContainsKey(k)
                    && finalList.Contains(k)
                );
                foreach (var animal in animalsWithoutEggRelations)
                {
                    SavedItemsData.AnimalItemRelation[animal] = new()
                    {
                        { CloneItemType.Egg, null },
                        { CloneItemType.Milk, null },
                        { CloneItemType.Misc, null }
                    };
                }

                //foreach (var item in collection) { }
                foreach (var animal in finalList)
                {
                    AddEggRelationsForAnimal(
                        animal,
                        animalDataDic,
                        category,
                        animalProducts,
                        lists,
                        cloneItemType
                    );
                }
            }
        }

        public static CloneItemType[] GetCloneItemTypes()
        {
            return new CloneItemType[]
            {
                CloneItemType.Egg,
                CloneItemType.Milk,
                CloneItemType.Misc
            };
        }

        private int GetCategoryPerCloneItemTypes(CloneItemType cloneItemType)
        {
            return cloneItemType switch
            {
                CloneItemType.Egg => Object.EggCategory,
                CloneItemType.Milk => Object.MilkCategory,
                CloneItemType.Misc => 0,
                _ => 0
            };
        }

        private string[] GetAnimalProductsPerCloneItemTypes(CloneItemType cloneItemType)
        {
            return cloneItemType switch
            {
                CloneItemType.Egg => AnimalProducts.VanillaEggs,
                CloneItemType.Milk => AnimalProducts.VanillaMilk,
                CloneItemType.Misc
                    => new string[] { AnimalProducts.DuckFeather, AnimalProducts.Wool },
                _ => Array.Empty<string>()
            };
        }

        private void UpdateEggRelations(string animalKey)
        {
            foreach (
                var item in SavedItemsData
                    .AnimalItemRelation[animalKey]
                    .Where(x => x.Value != null)
                    .Select(x => x.Value!)
            )
            {
                var normalItem = item[ItemType.Normal];
                var largeItem = item[ItemType.Large];
                if (!Game1.objectData.ContainsKey(normalItem) && normalItem != string.Empty)
                {
                    item[ItemType.Normal] = string.Empty;
                }
                if (!Game1.objectData.ContainsKey(largeItem) && largeItem != string.Empty)
                {
                    item[ItemType.Large] = string.Empty;
                }
            }
        }

        private void AddEggRelationsForAnimal(
            string animal,
            IDictionary<string, FarmAnimalData> animalDataDic,
            int category,
            string[] animalProducts,
            List<string>[] lists,
            CloneItemType cloneItemType
        )
        {
            DefaultValues.TryAdd(animal, new());
            DefaultValues[animal].TryAdd(cloneItemType, new());
            DefaultValues[animal][cloneItemType] = GetDefaultAnimalItem(
                animal,
                animalDataDic,
                category,
                animalProducts,
                lists,
                cloneItemType
            );
            SavedItemsData.AnimalItemRelation[animal][cloneItemType] ??= new(
                DefaultValues[animal][cloneItemType]
            );
        }

        private Dictionary<ItemType, string> GetDefaultAnimalItem(
            string animal,
            IDictionary<string, FarmAnimalData> animalDataDic,
            int category,
            string[] animalProducts,
            List<string>[] lists,
            CloneItemType cloneItemType
        )
        {
            // Destructure lists for better readability
            var (animalsWithVanillaEggs, animalsWithRepeatingEgg) = (lists[0], lists[1]);
            var data = animalDataDic[animal];
            var exclusions = GetExclusions().EditExclusions[EditType.CloneEggs];

            // Return empty dictionary if animal is excluded
            if (exclusions.Contains(animal))
            {
                return CreateEmptyEggDictionary();
            }

            // Determine egg type based on animal category
            if (animalsWithVanillaEggs.Contains(animal))
            {
                return CreateEggDictionary(
                    DetermineItemsToCloneFromProduce(data, animalProducts, cloneItemType)
                );
            }

            if (animalsWithRepeatingEgg.Contains(animal))
            {
                return CreateEggDictionary(
                    DetermineItemsToCloneFromRepeatingItem(
                        data,
                        category,
                        animalProducts,
                        cloneItemType
                    )
                );
            }

            // Default case: return empty dictionary
            return CreateEmptyEggDictionary();
        }

        // Helper method to create an empty egg dictionary
        private Dictionary<ItemType, string> CreateEmptyEggDictionary()
        {
            return new Dictionary<ItemType, string>
            {
                [ItemType.Normal] = string.Empty,
                [ItemType.Large] = string.Empty
            };
        }

        // Helper method to create an egg dictionary from a tuple
        private Dictionary<ItemType, string> CreateEggDictionary(
            (string normalEgg, string largeEgg) eggItems
        )
        {
            return new Dictionary<ItemType, string>
            {
                [ItemType.Normal] = eggItems.normalEgg,
                [ItemType.Large] = eggItems.largeEgg
            };
        }

        private (string, string) DetermineItemsToCloneFromProduce(
            FarmAnimalData data,
            string[] animalProducts,
            CloneItemType itemType
        )
        {
            var produce = data.ProduceItemIds.Union(data.DeluxeProduceItemIds);
            var itemsToClone = produce
                .Where(p => animalProducts.Contains(p.ItemId))
                .Select(p => p.ItemId)
                .Distinct();

            return itemType switch
            {
                CloneItemType.Egg
                    => HandleItemType(
                        itemsToClone,
                        AnimalProducts.LargeBrownEgg,
                        AnimalProducts.LargeWhiteEgg
                    ),
                CloneItemType.Milk
                    => HandleItemType(
                        itemsToClone,
                        AnimalProducts.LargeGoatMilk,
                        AnimalProducts.LargeMilk
                    ),
                _ => HandleDefaultType(itemsToClone),
            };
        }

        private (string, string) HandleItemType(
            IEnumerable<string> itemsToClone,
            string largeItem1,
            string largeItem2
        )
        {
            if (itemsToClone.Contains(largeItem1) || itemsToClone.Contains(largeItem2))
            {
                var largeItem = itemsToClone.Contains(largeItem1) ? largeItem1 : largeItem2;
                var normalItem = itemsToClone.FirstOrDefault(e => e != largeItem, string.Empty);
                return (normalItem, largeItem);
            }
            else
            {
                var normalItem = itemsToClone.First();
                var largeItem =
                    itemsToClone.Count() > 1 ? itemsToClone.Skip(1).First() : string.Empty;
                return (normalItem, largeItem);
            }
        }

        private (string, string) HandleDefaultType(IEnumerable<string> itemsToClone)
        {
            if (
                itemsToClone.Contains(AnimalProducts.DuckFeather)
                || itemsToClone.Contains(AnimalProducts.Wool)
            )
            {
                var normalItem = itemsToClone.Contains(AnimalProducts.DuckFeather)
                    ? AnimalProducts.DuckFeather
                    : AnimalProducts.Wool;
                var largeItem = itemsToClone.FirstOrDefault(e => e != normalItem, string.Empty);
                return (normalItem, largeItem);
            }
            else
            {
                var normalItem = itemsToClone.First();
                var largeItem =
                    itemsToClone.Count() > 1 ? itemsToClone.Skip(1).First() : string.Empty;
                return (normalItem, largeItem);
            }
        }

        private (string, string) DetermineItemsToCloneFromRepeatingItem(
            FarmAnimalData data,
            int category,
            string[] animalProducts,
            CloneItemType itemType
        )
        {
            data.ProduceItemIds ??= new();
            data.DeluxeProduceItemIds ??= new();
            var filterFunc =
                itemType == CloneItemType.Misc
                    ? GetMiscItemFilterFunc()
                    : GetVanillaItemFilterFunc();
            var produceItems = data
                .ProduceItemIds.Where(p => filterFunc(category, animalProducts, p))
                .Select(p => p.ItemId)
                .ToList();

            var deluxeProduceItems = data
                .DeluxeProduceItemIds.Where(p => filterFunc(category, animalProducts, p))
                .Select(p => p.ItemId)
                .ToList();

            if (produceItems.Any() && deluxeProduceItems.Any())
            {
                return (produceItems[0], deluxeProduceItems[0]);
            }
            else if (produceItems.Any())
            {
                return (produceItems[0], string.Empty);
            }
            else if (deluxeProduceItems.Any())
            {
                return (deluxeProduceItems[0], string.Empty);
            }
            else
            {
                return (string.Empty, string.Empty);
            }
        }

        private bool CheckIfProduceVanillaItem(FarmAnimalData animalData, string[] animalProducts)
        {
            animalData.ProduceItemIds ??= new();
            animalData.DeluxeProduceItemIds ??= new();
            if (animalData.ProduceItemIds.Any() || animalData.DeluxeProduceItemIds.Any())
            {
                var bothProducesList = animalData
                    .ProduceItemIds.Union(animalData.DeluxeProduceItemIds)
                    .ToList();
                return bothProducesList.Select(x => x.ItemId).Any(i => animalProducts.Contains(i));
            }
            else
            {
                return false;
            }
        }

        private bool CheckIfProduceAnyItemOfThisType(
            FarmAnimalData animalData,
            string[] animalProducts,
            int category
        )
        {
            animalData.ProduceItemIds ??= new();
            animalData.DeluxeProduceItemIds ??= new();
            if (animalData.ProduceItemIds.Any() || animalData.DeluxeProduceItemIds.Any())
            {
                var bothProducesList = animalData
                    .ProduceItemIds.Union(animalData.DeluxeProduceItemIds)
                    .ToList();
                return bothProducesList
                    .Select(x => x.ItemId)
                    .Any(i =>
                        Game1.objectData.ContainsKey(i)
                        && (animalProducts.Contains(i) || Game1.objectData[i].Category == category)
                    );
            }
            else
            {
                return false;
            }
        }

        private Func<int, string[], FarmAnimalProduce, bool> GetVanillaItemFilterFunc()
        {
            return (category, animalProducts, produce) =>
                Game1.objectData.TryGetValue(produce.ItemId, out var item)
                && item.Category == category
                && animalProducts.Contains(produce.ItemId)
                && !produce.ItemId.StartsWith(
                    ModEntry.ModManifestInstance.UniqueID ?? "",
                    StringComparison.OrdinalIgnoreCase
                );
        }

        private Func<int, string[], FarmAnimalProduce, bool> GetMiscItemFilterFunc()
        {
            return (_, _, produce) =>
                Game1.objectData.TryGetValue(produce.ItemId, out var item)
                && item.Category != Object.EggCategory
                && item.Category != Object.MilkCategory
                && (
                    !Game1.objectData.ContainsKey(produce.ItemId)
                    || produce.ItemId == AnimalProducts.Wool
                    || produce.ItemId == AnimalProducts.DuckFeather
                    || produce.ItemId == AnimalProducts.RabbitFoot
                )
                && !produce.ItemId.StartsWith(
                    ModEntry.ModManifestInstance.UniqueID,
                    StringComparison.OrdinalIgnoreCase
                );
        }

        private List<string> GetAnimalsWithRepeatingItems(
            IDictionary<string, FarmAnimalData> animalDataDic,
            int category,
            string[] animalProducts,
            CloneItemType cloneItemType
        )
        {
            var filterFunc =
                cloneItemType == CloneItemType.Misc
                    ? GetMiscItemFilterFunc()
                    : GetVanillaItemFilterFunc();
            var itemIds = new Dictionary<string, List<string>>();

            foreach (var animalDataKeyValuePair in animalDataDic)
            {
                var animalKey = animalDataKeyValuePair.Key;
                var animalData = animalDataKeyValuePair.Value;

                var allItemIds = animalData
                    .ProduceItemIds.Concat(animalData.DeluxeProduceItemIds ?? new())
                    .Where(p => filterFunc(category, animalProducts, p))
                    .Select(p => p.ItemId)
                    .ToList();

                foreach (var itemId in allItemIds)
                {
                    if (!itemIds.ContainsKey(itemId))
                    {
                        itemIds.Add(itemId, new List<string>());
                    }
                    itemIds[itemId].Add(animalKey);
                    SavedItemsData.FoundItems[cloneItemType].TryAdd(itemId);
                }
            }

            // Filter out items that are not repeated
            var repeatedItemIds = itemIds.Where(x => x.Value.Count > 1).Select(x => x.Key).ToList();

            // Get the animal keys that correspond to the repeated item IDs, skipping the first one and excluding VanillaAnimals
            var result = new List<string>();
            foreach (var itemId in repeatedItemIds)
            {
                var animalKeys = itemIds[itemId]
                    .Skip(1)
                    .Where(key =>
                        !VanillaAnimals.AnimalKeys.Contains(key) || key == VanillaAnimals.Rabbit
                    )
                    .ToList();
                result.AddRange(animalKeys);
            }

            return result;
        }

        private EditExclusion GetExclusions()
        {
            static List<string> BuildExclusionList(List<string> exclusions)
            {
                var AnimalExclusions = new List<string>();
                foreach (var animal in exclusions)
                {
                    if (animal.Contains(','))
                    {
                        AnimalExclusions.AddRange(
                            animal.Split(",", StringSplitOptions.TrimEntries)
                        );
                    }
                    else
                    {
                        AnimalExclusions.Add(animal);
                    }
                }
                return AnimalExclusions;
            }
            var exclusions = ModEntry.HelperInstance.Data.ReadJsonFile<EditExclusion>("data/exclusions.json");
            if (exclusions == null)
            {
                ModEntry.HelperInstance.Data.WriteJsonFile("data/exclusions.json", new EditExclusion());
                exclusions = ModEntry.HelperInstance.Data.ReadJsonFile<EditExclusion>("data/exclusions.json");
            }
            var Exclusions = exclusions;
            var finalExclusions = new EditExclusion();
            finalExclusions.EditExclusions[EditType.BirthText] = BuildExclusionList(
                Exclusions!
                    .EditExclusions[EditType.BirthText]
                    .Concat(VanillaAnimals.AnimalKeys)
                    .ToList()
            );
            finalExclusions.EditExclusions[EditType.EggList] = BuildExclusionList(
                Exclusions!
                    .EditExclusions[EditType.EggList]
                    .Concat(VanillaAnimals.AnimalKeys)
                    .ToList()
            );
            finalExclusions.EditExclusions[EditType.CloneEggs] = BuildExclusionList(
                Exclusions!
                    .EditExclusions[EditType.CloneEggs]
                    .ToList()
            );
            finalExclusions.ClonedEggsExclusionListEggs = BuildExclusionList(
                Exclusions!.ClonedEggsExclusionListEggs
            );
            return finalExclusions;
        }
    }
}
