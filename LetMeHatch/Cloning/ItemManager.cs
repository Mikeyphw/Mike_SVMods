using System;
using System.Text.RegularExpressions;
using LetMeHatch.GameAssetManagement;
using LetMeHatch.Breeding;
using LetMeHatch.Config;
using LetMeHatch.Framework;
using LetMeHatch.Helpers;
using LetMeHatch.Models;
using LetMeHatch.Core.ClonePipeline;
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;
using static LetMeHatch.EnumsAndStrings.Enums;
using static LetMeHatch.EnumsAndStrings.Strings;
using static Mikew.NestCore.EnumsAndStrings.Strings;
using static Mikew.NestCore.EnumsAndStrings.Strings.ItemIds;
using Object = StardewValley.Object;
using Mikew.NestCore.Helpers;

using static LetMeHatch.ModEntry;
using LetMeHatch.Utilities;

namespace LetMeHatch.Cloning
{
    public class ItemManager
    {
        // Dependencies
        private readonly Dictionary<
            string,
            Dictionary<string, BreedingDataFemale>
        > _loadedFemaleBreedingData;
        private readonly SavedItemsData _savedItemsData;
        private readonly SavedColors _savedColors;
        private readonly CloningHelper _cloningHelper;
        private readonly TextureManager _textureManager;
        private readonly Dictionary<
            string,
            Dictionary<CloneItemType, Dictionary<string, ObjectData>>
        > _itemsToAdd;
        private readonly List<OutputItemCloneInfo> _outpuItemsToClone;
        private readonly Dictionary<string, ObjectData> _fertilizedEggsToAdd;

        // Public Properties

        internal List<string> GameColorTags { get; set; } = new();

        public ItemManager(
            TextureManager textureManager,
            SavedItemsData savedItemsData,
            SavedColors savedColors,
            CloningHelper cloningHelper,
            Dictionary<string, Dictionary<string, BreedingDataFemale>> loadedFemaleBreedingData,
            Dictionary<
                string,
                Dictionary<CloneItemType, Dictionary<string, ObjectData>>
            > itemsToAdd,
            List<OutputItemCloneInfo> outpuItemsToClone,
            Dictionary<string, ObjectData> fertilizedEggsToAdd
        )
        {
            _textureManager = textureManager;
            _savedItemsData = savedItemsData;
            _savedColors = savedColors;
            _cloningHelper = cloningHelper;
            _loadedFemaleBreedingData = loadedFemaleBreedingData;
            _itemsToAdd = itemsToAdd;
            _fertilizedEggsToAdd = fertilizedEggsToAdd;
            _outpuItemsToClone = outpuItemsToClone;
        }

        #region Items
        public void CloneItemsFromGame()
        {
            foreach (var cloneItemType in AnimalItemRelationsManager.GetCloneItemTypes())
            {
                if (cloneItemType == CloneItemType.Egg && ModEntry.Config.UseDynamicCloneTextures)
                {
                    CloneEggsFromFarmAnimalProducePlan();
                    continue;
                }

                CloneEggsFromGame(cloneItemType);
            }
        }

        public void CloneEggsFromGame(CloneItemType cloneItemType)
        {
            foreach (
                var (animalKey, animalItems) in _savedItemsData
                    .AnimalItemRelation.Where(a => a.Value[cloneItemType] != null)
                    .Select(a => (a.Key, a.Value[cloneItemType]!))
            )
            {
                var gameFarmAnimaldata = Game1.farmAnimalData;
                if (
                    animalItems[ItemType.Normal] == string.Empty
                    && animalItems[ItemType.Large] == string.Empty
                )
                {
                    // LoggerHelper.VerboseLog(
                    //     $"Skipping {animalKey}'s {cloneItemType} cloning because of both items being empty!",
                    //     LogLevel.Alert
                    // );
                    continue;
                }
                var animal = gameFarmAnimaldata[animalKey];
                animal.CustomFields ??= new();
                animal.ProduceItemIds ??= new();
                animal.DeluxeProduceItemIds ??= new();
                animal.CustomFields[$"Cloned{cloneItemType}s"] = "";
                _itemsToAdd.TryAdd(animalKey, new());

                _itemsToAdd[animalKey][cloneItemType] = GetClonedItems(
                    animalKey,
                    animal,
                    animalItems,
                    gameFarmAnimaldata,
                    cloneItemType
                );
            }
        }

        public Dictionary<string, ObjectData> GetClonedItems(
            string animalKey,
            FarmAnimalData animalData,
            Dictionary<ItemType, string> animalItems,
            IDictionary<string, FarmAnimalData> gameFarmAnimaldata,
            CloneItemType cloneItemType
        )
        {
            var clonedItems = new Dictionary<string, ObjectData>();
            foreach (
                var (itemType, itemId) in animalItems.Where(e => !string.IsNullOrEmpty(e.Value))
            )
            {
                var itemData = CreateClonedItemData(
                    animalKey,
                    itemId,
                    itemType,
                    animalData,
                    gameFarmAnimaldata,
                    cloneItemType
                );
                clonedItems.Add(itemData.Key, itemData.Value);
                if (itemId == AnimalProducts.Wool)
                {
                    var clothData = CreateClonedItemData(
                        animalKey,
                        ArtisanGoods.Cloth,
                        itemType,
                        animalData,
                        gameFarmAnimaldata,
                        cloneItemType
                    );
                    _outpuItemsToClone.Add(
                        new(itemId, itemData.Key, ArtisanGoods.Cloth, clothData.Key)
                    );
                    clonedItems.Add(clothData.Key, clothData.Value);
                }
            }

            return clonedItems;
        }

        private KeyValuePair<string, ObjectData> CreateClonedItemData(
            string animalKey,
            string originalItemID,
            ItemType itemType,
            FarmAnimalData animalData,
            IDictionary<string, FarmAnimalData> gameFarmAnimaldata,
            CloneItemType cloneItemType
        )
        {
            var stringToExtractFrom =
                $"{animalData.ShopDescription ?? string.Empty} {animalData.DisplayName} {animalKey}";
            string? color = null;
            if (
                cloneItemType != CloneItemType.Milk
                || (cloneItemType == CloneItemType.Milk && ModEntry.Config.ColorClonedMilk)
            )
            {
                if (!_savedColors.ItemColors.ContainsKey(animalKey))
                {
                    ColorTheItem(
                        _savedColors,
                        ModEntry.Config,
                        stringToExtractFrom,
                        animalKey,
                        originalItemID
                    );
                }
                color = _savedColors.ItemColors[animalKey];
            }
            var originalItemData = Game1.objectData[originalItemID];
            ObjectData clonedItemData;
            if (ModEntry.Config.UseDynamicCloneTextures)
            {
                clonedItemData = CreateCloneFromBlueprint(animalKey, originalItemID, itemType, cloneItemType, originalItemData);
            }
            else
            {
                clonedItemData = originalItemData.CloneOrDeepClone(true)!;
            }
            SetItemDataProperties(
                clonedItemData,
                originalItemData,
                animalKey,
                originalItemID,
                itemType,
                gameFarmAnimaldata,
                cloneItemType
            );
            SetItemDataContextTags(clonedItemData, originalItemID, itemType, cloneItemType, color);
            SetItemDataCustomFields(
                clonedItemData,
                animalKey,
                originalItemID,
                cloneItemType,
                color
            );

            return new(
                _cloningHelper.GetClonedItemID(animalKey, originalItemID, cloneItemType, itemType),
                clonedItemData
            );
        }

        private void SetItemDataProperties(
            ObjectData clonedItemData,
            ObjectData originalItemData,
            string animalKey,
            string originalItemID,
            ItemType itemType,
            IDictionary<string, FarmAnimalData> gameFarmAnimaldata,
            CloneItemType cloneItemType
        )
        {
            var clonedItemID = _cloningHelper.GetClonedItemID(
                animalKey,
                originalItemID,
                cloneItemType,
                itemType
            );
            if (originalItemID == AnimalProducts.DinoEgg)
            {
                HandleDinosaurEgg(clonedItemData, originalItemData);
            }
            SetDescriptionAndDisplayName(clonedItemData, animalKey, originalItemID);
            _textureManager.SetTextureAndSpriteIndex(
                clonedItemData,
                originalItemData,
                animalKey,
                originalItemID,
                clonedItemID,
                cloneItemType
            );
            SetPrice(
                clonedItemData,
                animalKey,
                originalItemID,
                cloneItemType,
                itemType,
                gameFarmAnimaldata
            );
        }

        private void HandleDinosaurEgg(ObjectData clonedEggData, ObjectData originalEggData)
        {
            if (ModEntry.Config.DinoEggAsArtifact)
            {
                clonedEggData.Category = originalEggData.Category;
                clonedEggData.Type = originalEggData.Type;
                clonedEggData.ArtifactSpotChances = originalEggData.ArtifactSpotChances;
            }
            else
            {
                clonedEggData.Category = Object.EggCategory;
                clonedEggData.ArtifactSpotChances = null;
            }
        }

        private void SetDescriptionAndDisplayName(
            ObjectData clonedEggData,
            string animalKey,
            string originalItemID
        )
        {
            var originalItemDisplayName = GameDisplayNames.GetObjectDisplayName(originalItemID);
            var animalDisplayName = GameDisplayNames.GetAnimalDisplayName(animalKey);
            clonedEggData.DisplayName = $"{originalItemDisplayName}: {animalDisplayName}";
        }

        private void SetPrice(
            ObjectData eggData,
            string animalKey,
            string itemID,
            CloneItemType cloneItemType,
            ItemType itemType,
            IDictionary<string, FarmAnimalData> gameFarmAnimaldata
        )
        {
            var animalPrice = GetAnimalPrice(animalKey, gameFarmAnimaldata);
            var animalProduceDays = Game1.farmAnimalData[animalKey].DaysToProduce;
            var price = CalculateItemPrice(
                itemID,
                animalPrice,
                animalProduceDays,
                cloneItemType,
                itemType
            );
            eggData.Price = price;
        }

        private int GetAnimalPrice(
            string animalKey,
            IDictionary<string, FarmAnimalData> gameFarmAnimaldata
        )
        {
            var animalData = gameFarmAnimaldata[animalKey];
            return animalData.PurchasePrice == -1
                ? GetAlternatePurchasePrice(gameFarmAnimaldata, animalKey)
                : animalData.PurchasePrice;
        }

        private int GetAlternatePurchasePrice(
            IDictionary<string, FarmAnimalData> gameFarmAnimaldata,
            string animalKey
        )
        {
            return gameFarmAnimaldata
                .Select(a => a.Value)
                .Where(a =>
                    a.AlternatePurchaseTypes != null
                    && a.AlternatePurchaseTypes.SelectMany(ap => ap.AnimalIds).Contains(animalKey)
                )
                .Select(a => a.PurchasePrice)
                .SingleOrDefault(0);
        }

        private int CalculateItemPrice(
            string originalItemID,
            int animalPrice,
            int animalProduceDays,
            CloneItemType cloneItemType,
            ItemType itemType
        )
        {
            var price = cloneItemType switch
            {
                CloneItemType.Egg
                    => GetPrice_Egg(
                        originalItemID,
                        Game1.objectData,
                        itemType,
                        animalPrice,
                        animalProduceDays
                    ),
                CloneItemType.Milk
                    => GetPrice_Milk(
                        originalItemID,
                        Game1.objectData,
                        itemType,
                        animalPrice,
                        animalProduceDays
                    ),
                CloneItemType.Misc
                    => GetPrice_Misc(
                        originalItemID,
                        Game1.objectData,
                        itemType,
                        animalPrice,
                        animalProduceDays
                    ),
                _
                    => GetPrice_Egg(
                        originalItemID,
                        Game1.objectData,
                        itemType,
                        animalPrice,
                        animalProduceDays
                    ),
            };
            return 5 * (int)price / 5;
        }

        private void SetItemDataContextTags(
            ObjectData clonedItemData,
            string originalItemId,
            ItemType itemType,
            CloneItemType cloneItemType,
            string? color
        )
        {
            clonedItemData.ContextTags ??= new();
            clonedItemData.ContextTags.Add("cloned_item");
            clonedItemData.ContextTags.Add($"cloned_{cloneItemType.ToString().ToLower()}");
            if (itemType == ItemType.Large)
            {
                clonedItemData.ContextTags.Add($"large_{cloneItemType.ToString().ToLower()}_item");
            }
            if (color != null)
            {
                bool containsColor = _savedColors
                    ._colorDictionary.SelectMany(outerKvp => outerKvp.Value)
                    .Select(innerKvp => innerKvp.Value)
                    .Any(innermostKvp => innermostKvp == color);

                if (containsColor)
                {
                    if (originalItemId == AnimalProducts.VoidEgg)
                    {
                        var colorName = GetKeyByValueGalaxy(color);
                        var colorNameTag = $"color_{colorName.ToLowerInvariant()}";
                        clonedItemData.ContextTags.Add(colorNameTag);
                    }
                    else
                    {
                        var (colorName, colorHue) = GetKeysByValue(color);
                        var colorNameTag = $"color_{colorName.ToLowerInvariant()}";
                        var colorHueTag = $"color_{colorHue.ToLowerInvariant()}";

                        if (GameColorTags.Contains(colorHueTag))
                        {
                            clonedItemData.ContextTags.Add(colorHueTag);
                        }
                        else
                        {
                            clonedItemData.ContextTags.Add(colorNameTag);
                        }
                    }
                }
            }
        }

        public (string ColorName, string ColorHue) GetKeysByValue(string hexCode)
        {
            return _savedColors
                ._colorDictionary.SelectMany(kvp =>
                    kvp.Value.Where(innerValueKV => innerValueKV.Value == hexCode)
                        .Select(innerValueKV => (ColorName: kvp.Key, ColorHue: innerValueKV.Key))
                )
                .FirstOrDefault();
        }

        public string GetKeyByValueGalaxy(string hexCode)
        {
            return _savedColors
                .GalaxyColors.SelectMany(innerDict => innerDict.Value)
                .Where(kv => kv.Value == hexCode)
                .Select(kv => kv.Key.Split(' ')[0])
                .Single();
        }

        private void SetItemDataCustomFields(
            ObjectData itemData,
            string animalKey,
            string itemID,
            CloneItemType cloneItemType,
            string? color
        )
        {
            itemData.CustomFields = new()
            {
                ["animal"] = animalKey,
                [$"original{cloneItemType}"] = itemID,
            };
            if (color != null)
            {
                itemData.CustomFields[CustomFieldColorKey] = color;
            }
        }

        public static void ColorTheItem(
            SavedColors savedColors,
            ModConfig config,
            string animalString,
            string animalKey,
            string originalEggID
        )
        {
            var colorControl = new Colors(config, savedColors);
            if (!config.EggColors.ContainsKey(originalEggID))
            {
                config.EggColors[originalEggID] = savedColors._colorDictionary.ToDictionary(
                    x => x.Key,
                    x => true
                );
            }
            string color;
            if (config.EggColors_ColorObject.TryGetValue(originalEggID, out var forced) && forced != Color.Transparent)
            {
                color = ColorToHex(forced);
            }
            else
            {
                color = colorControl.GetColor(
                    originalEggID,
                    animalString,
                    config.LinkAnimalColorToEgg,
                    config.EggColors[originalEggID]
                );
            } /*
			if(animalKey == "Olive Egger")
			{
				SavedColors.EggColors[animalKey] = colorControl.GetRandomHexCodeFromPalettes(Monitor,
				new Dictionary<string, bool>{{"green", true}}, new Dictionary<string, bool>{{"dark", }})
			}*/
            if (config.ChangeEggColors)
            {
                savedColors.ItemColors[animalKey] = color;
            }
            else
            {
                if (!savedColors.ItemColors.TryGetValue(animalKey, out var existingColor))
                {
                    savedColors.ItemColors[animalKey] = color;
                }
                /* if (Convert.ToBoolean(ModEntry.Config.EggOptions[eggID][ColorOptions.ColorEdit]))
                         {
                             SavedColors.EggColors[animalKey] = Helpers.Colors.AdjustColor(
                                 SavedColors.EggColors[animalKey],
                                 float.Parse(ModEntry.Config.EggOptions[eggID][ColorOptions.Luminance]),
                                 float.Parse(ModEntry.Config.EggOptions[eggID][ColorOptions.Saturation])
                             );
                         }*/
            }

            //SAVE
        }

        private static string ColorToHex(Color color)
            => $"#{color.R:X2}{color.G:X2}{color.B:X2}".ToUpperInvariant();


        public void AddClonedItems(IDictionary<string, ObjectData> objectData)
        {
            foreach (var cloneItemType in AnimalItemRelationsManager.GetCloneItemTypes())
            {
                foreach (
                    var item in _itemsToAdd
                        .Where(i => i.Value.ContainsKey(cloneItemType))
                        .SelectMany(i => i.Value[cloneItemType])
                )
                {
                    objectData[item.Key] = item.Value;
                }
            }
        }

        private double GetPrice_Egg(
            string id,
            IDictionary<string, ObjectData> data,
            ItemType itemType,
            int animalPrice,
            int animalProduceDays
        )
        {
            var chickenPrice =
                Game1.farmAnimalData[VanillaAnimals.WhiteChicken].PurchasePrice == -1
                    ? DefaultChickenPrice
                    : Game1.farmAnimalData[VanillaAnimals.WhiteChicken].PurchasePrice;
            var chickenProduction = Game1.farmAnimalData[VanillaAnimals.WhiteChicken].DaysToProduce;
            // Handle special cases for eggs
            if (Regex.IsMatch(id, DuckEggPattern))
            {
                var duckPrice =
                    Game1.farmAnimalData[VanillaAnimals.Duck].PurchasePrice == -1
                        ? DefaultDuckPrice
                        : Game1.farmAnimalData[VanillaAnimals.Duck].PurchasePrice;
                var duckProduction = Game1.farmAnimalData[VanillaAnimals.Duck].DaysToProduce;
                return GetPrice(
                    animalPrice,
                    animalProduceDays,
                    data[id].Price,
                    duckProduction,
                    duckPrice,
                    itemType
                );
            }

            if (
                Regex.IsMatch(id, VoidEggPattern)
                || Regex.IsMatch(id, GoldenEggPattern)
                || Regex.IsMatch(id, OstrichEggPattern)
                || Regex.IsMatch(id, DinoEggPattern)
            )
            {
                var priceDifferenceMultiplier =
                    1
                    + (GetLargeIncrementValue(data, itemType) - 1)
                    + Math.Abs(
                        data[AnimalProducts.VoidEgg].Price - data[AnimalProducts.WhiteEgg].Price
                    ) / data[AnimalProducts.WhiteEgg].Price;
                return priceDifferenceMultiplier
                    * GetPrice(
                        animalPrice,
                        animalProduceDays,
                        data[id].Price,
                        chickenProduction,
                        chickenPrice,
                        itemType
                    );
            }

            if (Regex.IsMatch(id, SlimeEggPattern))
            {
                string slimeEggId = Regex.Match(id, SlimeEggPattern).Value;
                var slimePrice = CalculateTargetAnimalCost(
                    data[AnimalProducts.WhiteEgg].Price,
                    data[slimeEggId].Price,
                    (double)1 / 20
                );
                return GetPrice(
                    animalPrice,
                    animalProduceDays,
                    data[slimeEggId].Price,
                    (double)1 / 20,
                    slimePrice,
                    itemType
                );
            }
            // Get the base animal price and production days
            var baseAnimal = GetBaseAnimal(id);

            if (baseAnimal.Key != null && !Regex.IsMatch(id, EggPattern))
            {
                return GetPrice(
                    animalPrice,
                    animalProduceDays,
                    data[id].Price,
                    baseAnimal.Value.DaysToProduce,
                    baseAnimal.Value.PurchasePrice,
                    itemType
                );
            }
            // Default to chicken egg price

            return GetPrice(
                animalPrice,
                animalProduceDays,
                data[id].Price,
                chickenProduction,
                chickenPrice,
                itemType
            );
        }

        // Create a method to calculate the price of milk
        private double GetPrice_Milk(
            string id,
            IDictionary<string, ObjectData> data,
            ItemType itemType,
            int animalPrice,
            int animalProduceDays
        )
        {
            var cowPrice =
                Game1.farmAnimalData[VanillaAnimals.WhiteCow].PurchasePrice == -1
                    ? DefaultCowPrice
                    : Game1.farmAnimalData[VanillaAnimals.WhiteCow].PurchasePrice;
            var cowProduction = Game1.farmAnimalData[VanillaAnimals.WhiteCow].DaysToProduce;
            // Get the base animal price and production days
            var baseAnimal = GetBaseAnimal(id);
            // Handle special cases for milk
            if (Regex.IsMatch(id, GoatMilkPattern))
            {
                var goatPrice =
                    Game1.farmAnimalData[VanillaAnimals.Goat].PurchasePrice == -1
                        ? DefaultGoatPrice
                        : Game1.farmAnimalData[VanillaAnimals.Goat].PurchasePrice;
                var goatProduction = Game1.farmAnimalData[VanillaAnimals.Goat].DaysToProduce;
                return GetPrice(
                    animalPrice,
                    animalProduceDays,
                    data[id].Price,
                    goatProduction,
                    goatPrice,
                    itemType
                );
            }

            if (Regex.IsMatch(id, IridiumMilkPattern))
            {
                var priceDifferenceMultiplier =
                    1
                    + (GetLargeIncrementValue(data, itemType) - 1)
                    + Math.Abs(
                        data[AnimalProducts.VoidEgg].Price - data[AnimalProducts.WhiteEgg].Price
                    ) / data[AnimalProducts.WhiteEgg].Price;
                return priceDifferenceMultiplier
                    * GetPrice(
                        animalPrice,
                        animalProduceDays,
                        data[id].Price,
                        cowProduction,
                        cowPrice,
                        itemType
                    );
            }
            if (baseAnimal.Key != null && !Regex.IsMatch(id, MilkPattern))
            {
                return GetPrice(
                    animalPrice,
                    animalProduceDays,
                    data[id].Price,
                    baseAnimal.Value.DaysToProduce,
                    baseAnimal.Value.PurchasePrice,
                    itemType
                );
            }

            // Default to cow milk price
            return GetPrice(
                animalPrice,
                animalProduceDays,
                data[id].Price,
                cowProduction,
                cowPrice,
                itemType
            );
        }

        // Create a method to calculate the price of wool and duck feathers
        private double GetPrice_Misc(
            string id,
            IDictionary<string, ObjectData> data,
            ItemType itemType,
            int animalPrice,
            int animalProduceDays
        )
        {
            // Default to sheep wool price
            // Handle special cases for wool and duck feathers
            if (Regex.IsMatch(id, WoolPattern))
            {
                var sheepPrice =
                    Game1.farmAnimalData[VanillaAnimals.Sheep].PurchasePrice == -1
                        ? DefaultSheepPrice
                        : Game1.farmAnimalData[VanillaAnimals.Sheep].PurchasePrice;
                var sheepProduction = Game1.farmAnimalData[VanillaAnimals.Sheep].DaysToProduce;
                return GetPrice(
                    animalPrice,
                    animalProduceDays,
                    data[id].Price,
                    sheepProduction,
                    sheepPrice,
                    itemType
                );
            }
            // Get the base animal price and production days
            var baseAnimal = GetBaseAnimal(id);

            if (baseAnimal.Key != null && !Regex.IsMatch(id, DuckFeatherPattern))
            {
                return GetPrice(
                    animalPrice,
                    animalProduceDays,
                    data[id].Price,
                    baseAnimal.Value.DaysToProduce,
                    baseAnimal.Value.PurchasePrice,
                    itemType
                );
            }

            var duckPrice =
                Game1.farmAnimalData[VanillaAnimals.Duck].PurchasePrice == -1
                    ? DefaultDuckPrice
                    : Game1.farmAnimalData[VanillaAnimals.Duck].PurchasePrice;
            var duckProduction = Game1.farmAnimalData[VanillaAnimals.Duck].DaysToProduce;
            return GetPrice(
                animalPrice,
                animalProduceDays,
                data[id].Price,
                duckProduction,
                duckPrice,
                itemType
            );
        }

        // Create a method to get the base animal price and production days
        private KeyValuePair<string, FarmAnimalData> GetBaseAnimal(string id)
        {
            return Game1
                .farmAnimalData.ToList()
                .Find(x =>
                    x.Value.PurchasePrice != -1
                    && x.Value.ProduceItemIds != null
                    && x.Value.ProduceItemIds.Concat(x.Value.DeluxeProduceItemIds ?? new())
                        .Any(p => p.ItemId != null && p.ItemId == id)
                );
        }

        // Create a method to get the large increment value
        private double GetLargeIncrementValue(
            IDictionary<string, ObjectData> data,
            ItemType itemType
        )
        {
            return itemType == ItemType.Large
                ? (double)data[AnimalProducts.LargeMilk].Price / data[AnimalProducts.Milk].Price
                : 0;
        }

        public double GetPrice(
            double newAnimalCost,
            double newDaysToProduce,
            double eggValue,
            double originaldaysToProduce,
            double originalAnimalCost,
            ItemType itemType
        )
        {
            var largeIncrementValue =
                itemType == ItemType.Large ? GetLargeIncrementValue(Game1.objectData, itemType) : 0;
            // Calculate the price of the egg per day for the original animal
            double eggPricePerDay = eggValue / originaldaysToProduce;

            // Calculate the price of the egg for the new animal
            double newEggPrice =
                eggPricePerDay * newDaysToProduce * (newAnimalCost / originalAnimalCost);

            return newEggPrice * largeIncrementValue;
        }

        public static double CalculateTargetAnimalCost(
            double dailyEggValueFromBaseChicken,
            double dailyEggValueFromTargetAnimal,
            double targetEggProductionFrequency
        )
        {
            const double baseChickenCost = 800.0;
            return baseChickenCost
                * dailyEggValueFromTargetAnimal
                * targetEggProductionFrequency
                / dailyEggValueFromBaseChicken;
        }

        #endregion

        #region Eggs
        public void SetFertilizedEggs(IDictionary<string, ObjectData> objectData)
        {
            foreach (var breedingDataGroup in _loadedFemaleBreedingData.SelectMany(x => x.Value))
            {
                var femaleKey = breedingDataGroup.Key;
                foreach (var male in breedingDataGroup.Value.Males)
                {
                    if (BreedingDataLoader.ShouldUseMearasFertilizedEggs(male.Key, femaleKey))
                    {
                        continue;
                    }
                    var eggs = male.Value.GetEggIDs();
                    var originalEgg = eggs[ItemType.Large] ?? eggs[ItemType.Normal];
                    if (originalEgg != null)
                    {
                        if (!objectData.ContainsKey(originalEgg))
                        {
                            LoggerHelper.VerboseLog(
                                $"Couldn't create fertilized egg for male {male.Key} with original egg {originalEgg}",
                                LogLevel.Error
                            );
                            continue;
                        }
                        var (ClonedEggID, ClonedEggData) = GetFertilizedEgg(
                            objectData[originalEgg],
                            femaleKey,
                            originalEgg
                        );
                        _fertilizedEggsToAdd[ClonedEggID] = ClonedEggData;
                    }
                }
            }
        }

        public (string ClonedEggID, ObjectData ClonedEggData) GetFertilizedEgg(
            ObjectData originalEggData,
            string animalKey,
            string originalEggID
        )
        {
            var clonedEggID = _cloningHelper.GetFertilizedClonedEggID(animalKey, originalEggID);
            /*eggID = IsClonedEgg(eggID)
                      ? GetOriginalEggAndAnimalID(eggID, false).OriginalEggID
                      : eggID;*/
            var clonedEggData = CreateEggData(originalEggData, animalKey, originalEggID);
            HandleContextTags(clonedEggData, originalEggData, originalEggID);
            if (CloningHelper.IsClonedEgg(originalEggID))
            {
                SetCustomFields(
                    clonedEggData,
                    clonedEggData.CustomFields["animal"],
                    originalEggID,
                    clonedEggData.CustomFields[CustomFieldColorKey]
                );
            }
            else
            {
                HandleNonClonedEgg(originalEggData, clonedEggData, animalKey, originalEggID);
            }

            return (clonedEggID, clonedEggData);
        }

        private void HandleContextTags(
            ObjectData clonedEggData,
            ObjectData originalEggData,
            string originalEggID
        )
        {
            clonedEggData.ContextTags =
                originalEggData.ContextTags != null ? new(originalEggData.ContextTags) : new();
            clonedEggData.ContextTags.Remove("cloned_egg");
            clonedEggData.ContextTags.Add("fertilized_egg");
            clonedEggData.CustomFields =
                originalEggData.CustomFields != null ? new(originalEggData.CustomFields) : new();
            clonedEggData.CustomFields["originalEgg"] = originalEggID;
        }

        private void HandleNonClonedEgg(
            ObjectData originalEggData,
            ObjectData clonedEggData,
            string animalKey,
            string originalEggID
        )
        {
            clonedEggData.ContextTags ??= new();

            var color = GetColorFromTags(originalEggData.ContextTags);
            clonedEggData.ContextTags =
                originalEggData.ContextTags == null
                    ? new()
                    : originalEggData.ContextTags.Where(s => !s.StartsWith("color_")).ToList();
            clonedEggData.CustomFields = new()
            {
                ["animal"] = animalKey,
                ["originalEgg"] = originalEggID
            };
            color ??= FindColor(clonedEggData, animalKey, originalEggID);

            clonedEggData.CustomFields["JollyLlama.PrismaticValleyFramework/Color"] =
                "Custom Palette";
            clonedEggData.CustomFields["JollyLlama.PrismaticValleyFramework/Palette"] =
                Colors.GenerateGlowingColorGradient(color, 3);
        }

        private string FindColor(ObjectData clonedEggData, string animalKey, string originalEggID)
        {
            var stringToExtractFrom =
                $"{Game1.farmAnimalData[animalKey].ShopDescription ?? string.Empty} {Game1.farmAnimalData[animalKey].DisplayName} {animalKey}";
            ColorTheItem(_savedColors, ModEntry.Config, stringToExtractFrom, animalKey, originalEggID);
            var color = _savedColors.ItemColors[animalKey];

            var colorTag = GetColorTag(color);
            if (colorTag != null)
            {
                clonedEggData.ContextTags.Add(colorTag);
            }
            return color;
        }

        private string? GetColorTag(string color)
        {
            bool containsColor = _savedColors
                ._colorDictionary.SelectMany(kvp => kvp.Value)
                .Any(innerKvp => innerKvp.Value == color);

            if (containsColor)
            {
                var (colorName, colorHue) = GetKeysByValue(color);
                var colorNameTag = $"color_{colorName.ToLowerInvariant()}";
                var colorHueTag = $"color_{colorHue.ToLowerInvariant()}";
                return GameColorTags.Contains(colorHueTag) ? colorHueTag : colorNameTag;
            }
            return null;
        }

        private void SetCustomFields(
            ObjectData eggData,
            string animalKey,
            string eggID,
            string color
        )
        {
            eggData.CustomFields = new()
            {
                ["animal"] = animalKey,
                ["originalEgg"] = eggID,
                ["JollyLlama.PrismaticValleyFramework/Color"] = "Custom Palette",
                ["JollyLlama.PrismaticValleyFramework/Palette"] =
                    Colors.GenerateGlowingColorGradient(color, 3)
            };
        }

        private string? GetColorFromTags(List<string> contextTags)
        {
            if (contextTags == null)
            {
                return null;
            }
            var color = contextTags.Count <= 0 ? null : Colors.GetColorFromTags(contextTags);

            return color;
        }

        private ObjectData CreateEggData(
            ObjectData originalEggData,
            string animalKey,
            string originalEggID
        )
        {
            var fertilizedEggID = _cloningHelper.GetFertilizedClonedEggID(animalKey, originalEggID);
            var clonedEggData = new ObjectData
            {
                Name = fertilizedEggID,
                DisplayName = $"{TranslationHelper.Get("fertilizedEgg.name")}: {animalKey}",
                Description = TranslationHelper.Get("fertilizedEgg.description"),
                Type = "BasicType",
                Category = StardewValley.Object.EggCategory,
                Price = originalEggData.Price * FertilizedPriceMultiplier,
                Edibility = originalEggData.Edibility + FertilizedEdibilityIncrement,
                CanBeGivenAsGift = originalEggData.CanBeGivenAsGift,
                CanBeTrashed = originalEggData.CanBeTrashed,
                ExcludeFromShippingCollection = originalEggData.ExcludeFromShippingCollection,
                ContextTags = new(),
                CustomFields = new()
            };

            _textureManager.SetTextureAndSpriteIndex(
                clonedEggData,
                originalEggData,
                animalKey,
                originalEggID,
                fertilizedEggID,
                CloneItemType.Egg,
                true
            );

            return clonedEggData;
        }

        public void ReplaceOriginalItemForClone(
            IDictionary<string, FarmAnimalData> gameFarmAnimaldata,
            CloneItemType cloneItemType
        )
        {
            foreach (
                var (animalKey, itemsToAdd) in _itemsToAdd
                    .Where(i => i.Value.ContainsKey(cloneItemType))
                    .Select(i => (i.Key, i.Value[cloneItemType]))
            )
            {
                var animal = gameFarmAnimaldata[animalKey];
                var animalProducts = _savedItemsData.FoundItems[cloneItemType];
                foreach (var (itemID, _) in itemsToAdd)
                {
                    var originalKey = _cloningHelper
                        .GetOriginalItemAndAnimalID(itemID)
                        .OriginalItemID;

                    if (cloneItemType == CloneItemType.Egg && animal.EggItemIds != null)
                    {
                        animal.EggItemIds.RemoveAll(e =>
                            e.ToLower() == originalKey.ToLower() || animalProducts.Contains(e)
                        );
                        animal.EggItemIds.Add(itemID);
                    }
                    foreach (
                        var produceItemId in animal.ProduceItemIds.Where(p =>
                            p.ItemId != null
                            && (
                                p.ItemId.ToLower() == originalKey.ToLower()
                                || animalProducts.Contains(p.ItemId)
                            )
                        )
                    )
                    {
                        produceItemId.ItemId = itemID;
                    }
                    foreach (
                        var deluxeProduceItemId in animal.DeluxeProduceItemIds.Where(p =>
                            p.ItemId != null
                            && (
                                p.ItemId.ToLower() == originalKey.ToLower()
                                || animalProducts.Contains(p.ItemId)
                            )
                        )
                    )
                    {
                        deluxeProduceItemId.ItemId = itemID;
                    }
                }
            }
        }

        private void SyncEggColors(IDictionary<string, ObjectData> objectData)
        {
            string pattern = @"^Mikew\.letmehatch\.smapi\.(.*)_(\d+)$";
            string patternEgg = @"(^.*_)(\d+)$";
            var normalEggs = new List<string> { AnimalProducts.WhiteEgg, AnimalProducts.BrownEgg };
            foreach (
                var normalEgg in objectData
                    .Keys.Where(obj => Regex.Match(obj, pattern).Success)
                    .Where(o => normalEggs.Contains(Regex.Replace(o, patternEgg, "$2")))
            )
            {
                var largeEgg = Regex.Replace(normalEgg, patternEgg, "$2") switch
                {
                    AnimalProducts.WhiteEgg => AnimalProducts.LargeWhiteEgg,
                    _ => AnimalProducts.LargeBrownEgg
                };
                var largeEggID = Regex.Replace(normalEgg, patternEgg, "$1") + largeEgg;
                if (objectData.ContainsKey(largeEggID))
                {
                    objectData[largeEggID].CustomFields[CustomFieldColorKey] = objectData[
                        normalEgg
                    ].CustomFields[CustomFieldColorKey];
                    objectData[largeEggID].ContextTags = objectData[largeEggID]
                        .ContextTags.Where(s => !s.StartsWith("color_"))
                        .ToList();
                    objectData[largeEggID]
                        .ContextTags.AddRange(
                            objectData[normalEgg].ContextTags.Where(s => s.StartsWith("color_"))
                        );
                }
            }
        }

        public void AddFertilizedEggs(IDictionary<string, ObjectData> data)
        {
            foreach (var (eggID, eggData) in _fertilizedEggsToAdd)
            {
                data[eggID] = eggData;
            }
        }

        public void EditFancyChickensEggs(IDictionary<string, FarmAnimalData> data)
        {
            var fancyChickensKeys = new List<string>
            {
                "Cuckoo Bluebar",
                "Cream Legbar",
                "Lavender Americauna",
                "Dark Olive Egger",
                "Olive Egger",
                "Light Olive Egger",
                "Asil",
                "Buff Orpington",
                "Light Sussex",
                "Salmon Faverolle",
                "Light Easter Egger",
                "Dark Easter Egger",
                "Easter Egger"
            };

            data["Cuckoo Bluebar"].PurchasePrice = 2500;

            foreach (var (_, animalData) in data.Where(a => fancyChickensKeys.Contains(a.Key)))
            {
                UpdateAnimalData(animalData);
            }
        }

        private void UpdateAnimalData(FarmAnimalData animalData)
        {
            animalData.EggItemIds ??= new List<string>();
            animalData.EggItemIds.Clear();
            animalData.ProduceItemIds.Clear();
            animalData.ProduceItemIds.Add(new FarmAnimalProduce() { Id = "176", ItemId = "176" });
            animalData.DeluxeProduceItemIds.Clear();
            animalData.DeluxeProduceItemIds.Add(
                new FarmAnimalProduce() { Id = "174", ItemId = "174" }
            );
        }

        public void RemoveFancyChickensEggs(IDictionary<string, ObjectData> data)
        {
            var fancyChickensEggs = new List<string>
            {
                "cantabilechaos.BFAVChickens_LightBlueEgg",
                "cantabilechaos.BFAVChickens_LargeLightBlueEgg",
                "cantabilechaos.BFAVChickens_OliveEgg",
                "cantabilechaos.BFAVChickens_LargeOliveEgg",
                "cantabilechaos.BFAVChickens_PinkEgg",
                "cantabilechaos.BFAVChickens_LargePinkEgg"
            };

            foreach (var objectKey in data.Keys.Where(o => fancyChickensEggs.Contains(o)).ToList())
            {
                data.Remove(objectKey);
            }
        }

        public void EditNewClonedEggs(IDictionary<string, ObjectData> data)
        {
            AddClonedItems(data);
        }

        public void EditNewFertilizedEggs(IDictionary<string, ObjectData> data)
        {
            if (_fertilizedEggsToAdd.Count < 1)
            {
                SetFertilizedEggs(data);
            }
            AddFertilizedEggs(data);
        }

        #endregion
    }
}
