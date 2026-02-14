using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;
using StardewValley;
using Mikew.BirdsOfAFeather.Helpers;
using StardewValley.GameData.FarmAnimals;
using WildEggsHatching.Framework;
using WildEggsHatching.Models;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings;
using static Mikew.BirdsOfAFeather.Helpers.ModAssetHandler;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.Managers.GameAssetManagement
{
    /// <summary>The mod entry point.</summary>
    ///
    internal class FarmAnimalEditor
    {
        private readonly WildListManager _wildListManager;
        private readonly ILetMeHatchApi? _letMeHatchApi;
        private readonly ModData _modSaveData;
        private readonly HashSet<WildEgg> _wildEggs;

        internal FarmAnimalEditor(
            WildListManager wildAnimalManager,
            HashSet<WildEgg> wildEggs,
            ILetMeHatchApi? letMeHatchApi,
            ModData modData
        )
        {
            _wildListManager = wildAnimalManager;
            _wildEggs = wildEggs;
            _letMeHatchApi = letMeHatchApi;
            _modSaveData = modData;
        }

        /// <summary>
        /// Adds vanilla egg prices to the given animal data.
        /// </summary>
        /// <param name="animalData">The dictionary containing animal data.</param>
        internal void AddVanillaAnimalPrices(IDictionary<string, FarmAnimalData> animalData)
        {
            if (!_wildListManager.ValidWildAnimals.Any())
            {
                return;
            }
            SetDefaultVoidChickenPrice(animalData);
            var vanillaAnimals = GetVanillaAnimals();

            foreach (var (animalKey, animalPrice) in vanillaAnimals.Where(a => a.Value != -1))
            {
                TryToUpdateAnimalPrice(animalData, animalKey, animalPrice);
            }
        }

        private void SetDefaultVoidChickenPrice(IDictionary<string, FarmAnimalData> animalData)
        {
            if (
                ModEntry.Config.VoidChickenPrice != "default"
                || !_wildListManager.ValidWildAnimals.Any()
            )
            {
                return;
            }
            var animalOnList = _wildListManager.ValidWildAnimals.First(a =>
                a.Name == VanillaAnimals.VoidChicken
            );

            var chickenPrice = (float)animalData[VanillaAnimals.WhiteChicken].PurchasePrice;
            var chickenEggPrice = (float)Game1.objectData["176"].Price;
            var voidEggPrice = (float)Game1.objectData["305"].Price;
            if (animalOnList == null)
            {
                return;
            }
            var voidChickenPrice = (int)(voidEggPrice / chickenEggPrice * chickenPrice / 5);
            voidChickenPrice *= 5;
            animalOnList.Price = voidChickenPrice * 2;
            ModEntry.Config.VoidChickenPrice = (voidChickenPrice * 2).ToString();
            ModEntry.HelperInstance.WriteConfig(ModEntry.Config);
        }

        private Dictionary<string, int> GetVanillaAnimals()
        {
            return new Dictionary<string, int>()
            {
                { VanillaAnimals.Dinosaur, GetConfigPrice(ModEntry.Config.DinoPrice) },
                { VanillaAnimals.VoidChicken, GetConfigPrice(ModEntry.Config.VoidChickenPrice) },
                { VanillaAnimals.Ostrich, GetConfigPrice(ModEntry.Config.OstrichPrice) },
            };
        }

        private int GetConfigPrice(string configValue)
        {
            if (int.TryParse(configValue, out int price))
            {
                return price;
            }
            throw new InvalidOperationException($"Invalid price: {configValue}");
        }

        private void TryToUpdateAnimalPrice(
            IDictionary<string, FarmAnimalData> animalData,
            string animalKey,
            int animalPrice
        )
        {
            var currentAnimalData = animalData[animalKey];
            if (currentAnimalData.PurchasePrice == -1)
            {
                UpdateAnimalPrices(currentAnimalData, animalPrice, animalKey);
            }
            else
            {
                UpdateConfigPrice(animalKey, currentAnimalData.PurchasePrice);
                LoggerHelper.VerboseLog(
                    $"Another mod has already made the {animalKey} available at the shop.",
                    LogLevel.Warn
                );
            }
        }

        private void UpdateAnimalPrices(
            FarmAnimalData animalData,
            int animalPrice,
            string animalKey
        )
        {
            var price = CalculatePrice(animalPrice);
            animalData.PurchasePrice = price;
            animalData.SellPrice = price;
            animalData.ShopDisplayName ??= animalData.DisplayName;
            animalData.ShopDescription ??= TranslationHelper.Get(
                $"shop.description.{animalKey.Replace(" ", "")}"
            );
            animalData.ShopTexture ??= AssetHandler.GetAssetPath(
                $"{animalKey.Replace(" ", "")}shopIcon",
                AssetPathType.GamePath
            );
        }

        private int CalculatePrice(int animalPrice)
        {
            return (int)(animalPrice / 2 / 5) * 5;
        }

        private void UpdateConfigPrice(string animalKey, int purchasePrice)
        {
            switch (animalKey)
            {
                case VanillaAnimals.Dinosaur:
                    ModEntry.Config.DinoPrice = (purchasePrice * 2).ToString();
                    break;
                case VanillaAnimals.VoidChicken:
                    ModEntry.Config.VoidChickenPrice = (purchasePrice * 2).ToString();
                    break;
                case VanillaAnimals.Ostrich:
                    ModEntry.Config.OstrichPrice = (purchasePrice * 2).ToString();
                    break;
                default:
                    break;
            }
            ModEntry.HelperInstance.WriteConfig(ModEntry.Config);
        }

        /// <summary>
        /// Adds wild eggs to the specified chosen animals in the provided animal data.
        /// This method adds eggs to both regular and magic animals associated with the Coop and Barn buildings.
        /// </summary>
        /// <param name="animalData">The dictionary containing animal data, where the key is the animal identifier and the value is the animal data.</param>
        internal void AddWildEggsToChosenAnimals(IDictionary<string, FarmAnimalData> animalData)
        {
            foreach (var egg in _wildEggs.Where(e => e.ChosenAnimalKey != null))
            {
                AddEggToAnimal(animalData, egg);
            }
        }

        private void AddEggToAnimal(IDictionary<string, FarmAnimalData> data, WildEgg wildEgg)
        {
            if (string.IsNullOrEmpty(wildEgg.ChosenAnimalKey))
            {
                return;
            }
            string eggId = wildEgg.ID;
            string animalId = wildEgg.ChosenAnimalKey;
            string home = wildEgg.IsLarge ? "Barn" : "Coop";

            var animalData = data.FirstOrDefault(a =>
                a.Key.Equals(animalId, StringComparison.Ordinal)
            ).Value;
            if (animalData != null)
            {
                animalData.EggItemIds ??= new();
                animalData.EggItemIds.Add(eggId);
                animalData.House = $"{home}";
                animalData.IncubationTime = 1000;
                LoggerHelper.VerboseLog($"Added {eggId} to {animalId} egg list.");
            }
            else
            {
                LoggerHelper.VerboseLog($"Animal not found: {animalId}", LogLevel.Warn);
            }
        }

        // private void ProgressionModeEdits(IDictionary<string, FarmAnimalData> animalData)
        // {
        //     const string BlueChicken = VanillaAnimals.BlueChicken;
        //     var canAlreadyBuyBlueChicken = animalData[BlueChicken].PurchasePrice != -1;
        //     if (!_wildAnimalManager.LoadedAnimalsKeys.Any(a => animalData.ContainsKey(a)))
        //     {
        //         LoggerHelper.VerboseLog("Not constains animaaaaal", LogLevel.Error);
        //     }
        //     var originalAnimalData = new Dictionary<string, FarmAnimalData>();
        //     foreach (
        //         var animal in _wildAnimalManager.LoadedAnimalsKeys.Where(a =>
        //             animalData.ContainsKey(a)
        //         )
        //     )
        //     {
        //         originalAnimalData[animal] = new FarmAnimalData()
        //         {
        //             PurchasePrice = animalData[animal].PurchasePrice,
        //             AlternatePurchaseTypes = new List<AlternatePurchaseAnimals>()
        //         };

        //         if (animalData[animal].AlternatePurchaseTypes != null)
        //         {
        //             foreach (var purchaseType in animalData[animal].AlternatePurchaseTypes)
        //             {
        //                 originalAnimalData[animal]
        //                     .AlternatePurchaseTypes.Add(
        //                         new AlternatePurchaseAnimals
        //                         {
        //                             AnimalIds = new List<string>(purchaseType.AnimalIds),
        //                             Condition = purchaseType.Condition,
        //                             Id = purchaseType.Id
        //                         }
        //                     );
        //                 /*  LoggerHelper.VerboseLog(
        //                       $"Added purchase type {purchaseType.Id} to animal {animal}",
        //                       LogLevel.Info
        //                   );*/
        //             }
        //         }
        //     }
        //     var hiddenAnimals = new List<(string AnimalKey, string? MasterAnimal)>();
        //     var avaliableAlternateAnimals = new List<string>();
        //     var avaliableAnimals = new List<string>();
        //     foreach (
        //         var animal in _wildAnimalManager.LoadedAnimalsKeys.Where(a =>
        //             animalData.ContainsKey(a)
        //         )
        //     )
        //     {
        //         var OriginalPurchaseAnimal = originalAnimalData
        //             .GetOriginalPurchaseAnimal(animal)
        //             ?.Key;

        //         if (ShouldBeAdded(animal))
        //         {
        //             if (OriginalPurchaseAnimal != null && !ShouldBeAdded(OriginalPurchaseAnimal))
        //             {
        //                 avaliableAlternateAnimals.Add(animal);
        //                 if (animal == BlueChicken && canAlreadyBuyBlueChicken)
        //                 {
        //                     OriginalPurchaseAnimal = animal;
        //                 }
        //                 var currentAnimalData = animalData[animal];
        //                 var originalPurchaseAnimalData = originalAnimalData[OriginalPurchaseAnimal];
        //                 currentAnimalData.PurchasePrice = originalPurchaseAnimalData.PurchasePrice;
        //                 currentAnimalData.RequiredBuilding =
        //                     originalPurchaseAnimalData.RequiredBuilding;
        //                 currentAnimalData.ShopDescription =
        //                     originalPurchaseAnimalData.ShopDescription;
        //                 currentAnimalData.ShopDisplayName =
        //                     originalPurchaseAnimalData.ShopDisplayName;
        //                 currentAnimalData.ShopMissingBuildingDescription =
        //                     originalPurchaseAnimalData.ShopMissingBuildingDescription;
        //                 currentAnimalData.ShopSourceRect =
        //                     originalPurchaseAnimalData.ShopSourceRect;
        //                 currentAnimalData.ShopTexture = originalPurchaseAnimalData.ShopTexture;
        //                 currentAnimalData.AlternatePurchaseTypes =
        //                     originalPurchaseAnimalData.AlternatePurchaseTypes;
        //                 if (animal != OriginalPurchaseAnimal)
        //                 {
        //                     var alternatePurchaseEntry =
        //                         originalPurchaseAnimalData.AlternatePurchaseTypes.Find(ap =>
        //                             ap.AnimalIds.Contains(animal)
        //                         );

        //                     if (
        //                         alternatePurchaseEntry != null
        //                         && alternatePurchaseEntry.Condition != null
        //                     )
        //                     {
        //                         currentAnimalData.UnlockCondition = string.IsNullOrEmpty(
        //                             originalPurchaseAnimalData.UnlockCondition
        //                         )
        //                             ? alternatePurchaseEntry.Condition
        //                             : $"{originalPurchaseAnimalData.UnlockCondition}, {alternatePurchaseEntry.Condition}";
        //                     }
        //                     else
        //                     {
        //                         currentAnimalData.UnlockCondition =
        //                             originalPurchaseAnimalData.UnlockCondition;
        //                     }
        //                 }
        //                 else
        //                 {
        //                     currentAnimalData.UnlockCondition =
        //                         originalPurchaseAnimalData.UnlockCondition;
        //                 }
        //             }
        //         }
        //         else
        //         {
        //             animalData[animal].PurchasePrice = -1;

        //             var clonedAnimalInfo = _letMeHatchApi?.ClonedAnimalsInfo.Find(x =>
        //                 x.FemaleKey == animal
        //             );
        //             if (clonedAnimalInfo.HasValue)
        //             {
        //                 var maleKey = clonedAnimalInfo.Value.MaleKey;
        //                 LoggerHelper.VerboseLog(
        //                     $"Removed {maleKey} from the shop because it's a cloned animal."
        //                 );
        //                 if (
        //                     clonedAnimalInfo.Value.AddAsAlternatePurchase
        //                     && animalData[animal].AlternatePurchaseTypes != null
        //                 )
        //                 {
        //                     animalData[animal]
        //                         .AlternatePurchaseTypes.RemoveAll(ap =>
        //                             ap.AnimalIds.Contains(maleKey)
        //                         );
        //                 }
        //                 else
        //                 {
        //                     animalData[animal].PurchasePrice = -1;
        //                 }
        //             }
        //             hiddenAnimals.Add((animal, OriginalPurchaseAnimal));
        //             //LoggerHelper.VerboseLog($"Set animal {animal} purchase price to -1", LogLevel.Info);

        //             if (OriginalPurchaseAnimal != null)
        //             {
        //                 animalData[OriginalPurchaseAnimal]
        //                     .AlternatePurchaseTypes.ForEach(ap => ap.AnimalIds.Remove(animal));
        //                 /*LoggerHelper.VerboseLog(
        //                     $"Removed animal {animal} from alternate purchase types",
        //                     LogLevel.Info
        //                 );*/
        //             }
        //         }
        //     }
        //     /*  avaliableAnimals = wildAnimalManager
        //           .LoadedAnimalsKeys.Where(a => animalData.ContainsKey(a))
        //           .Except(hiddenAnimals.Select(a => a.AnimalKey))
        //           .ToList();
        //       var hiddenAnimalsString =
        //           $"Hidden animals: [{string.Join("\n", hiddenAnimals.Select(h => $"{h.AnimalKey}{(h.MasterAnimal != null ? $", alternate purchase of {h.MasterAnimal}" : "")}"))}]";
        //       var avaliableAnimalsString =
        //           $"Avaliable animals: [{string.Join("\n", avaliableAnimals.Select(a => $"{a}{(avaliableAlternateAnimals.Contains(a) ? ", had to create animal shop entry." : "")}"))}]";
        //       LoggerHelper.VerboseLog(hiddenAnimalsString);
        //       LoggerHelper.VerboseLog(avaliableAnimalsString);*/
        // }

        // internal bool ShouldBeAdded(string animalKey)
        // {
        //     return _modSaveData.NewAddedAnimals.Select(nA => nA.FarmAnimalName).Contains(animalKey)
        //         || !ModEntry.Config.AnimalProgressionMode[animalKey];
        // }
        internal void ProgressionModeEdits(IDictionary<string, FarmAnimalData> animalData)
        {
            const string BlueChicken = VanillaAnimals.BlueChicken;
            var canAlreadyBuyBlueChicken = animalData[BlueChicken].PurchasePrice != -1;

            if (!_wildListManager.LoadedAnimalsKeys.Any(a => animalData.ContainsKey(a)))
            {
                LoggerHelper.VerboseLog("No animals found", LogLevel.Error);
                return;
            }

            var originalAnimalData = CloneOriginalAnimalData(animalData);
            var hiddenAnimals = new List<(string AnimalKey, string? MasterAnimal)>();
            var availableAlternateAnimals = new List<string>();

            foreach (var animal in _wildListManager.LoadedAnimalsKeys.Where(animalData.ContainsKey))
            {
                ProcessAnimal(
                    animal,
                    animalData,
                    originalAnimalData,
                    hiddenAnimals,
                    availableAlternateAnimals,
                    canAlreadyBuyBlueChicken
                );
            }

            // Optional: Log hidden and available animals if needed
            LogAnimalStatus(hiddenAnimals, availableAlternateAnimals);
        }

        private Dictionary<string, FarmAnimalData> CloneOriginalAnimalData(
            IDictionary<string, FarmAnimalData> animalData
        )
        {
            var originalAnimalData = new Dictionary<string, FarmAnimalData>();

            foreach (var animal in _wildListManager.LoadedAnimalsKeys.Where(animalData.ContainsKey))
            {
                originalAnimalData[animal] = animalData[animal].CloneOrDeepClone(true) ?? new();
            }

            return originalAnimalData;
        }

        private void ProcessAnimal(
            string animal,
            IDictionary<string, FarmAnimalData> animalData,
            IDictionary<string, FarmAnimalData> originalAnimalData,
            List<(string AnimalKey, string? MasterAnimal)> hiddenAnimals,
            List<string> availableAlternateAnimals,
            bool canAlreadyBuyBlueChicken
        )
        {
            LoggerHelper.VerboseLog($"Processing animal: {animal}", LogLevel.Trace, false);

            var originalPurchaseAnimal = originalAnimalData.GetOriginalPurchaseAnimal(animal)?.Key;
            LoggerHelper.VerboseLog(
                $"Original purchase animal for {animal}: {originalPurchaseAnimal}",
                LogLevel.Trace,
                false
            );

            if (ShouldBeAdded(animal))
            {
                LoggerHelper.VerboseLog($"Animal {animal} should be added.", LogLevel.Trace, false);

                UpdateAnimalData(
                    animal,
                    animalData,
                    originalAnimalData,
                    originalPurchaseAnimal,
                    canAlreadyBuyBlueChicken,
                    availableAlternateAnimals
                );

                LoggerHelper.VerboseLog(
                    $"Animal data updated for {animal}.",
                    LogLevel.Trace,
                    false
                );
            }
            else
            {
                LoggerHelper.VerboseLog(
                    $"Animal {animal} is hidden. Handling hidden animal.",
                    LogLevel.Trace,
                    false
                );

                HandleHiddenAnimal(animal, animalData, originalPurchaseAnimal, hiddenAnimals);

                LoggerHelper.VerboseLog($"Handled hidden animal: {animal}", LogLevel.Trace, false);
            }
        }

        private void UpdateAnimalData(
            string animal,
            IDictionary<string, FarmAnimalData> animalData,
            IDictionary<string, FarmAnimalData> originalAnimalData,
            string? originalPurchaseAnimal,
            bool canAlreadyBuyBlueChicken,
            List<string> availableAlternateAnimals
        )
        {
            if (originalPurchaseAnimal != null && !ShouldBeAdded(originalPurchaseAnimal))
            {
                availableAlternateAnimals.Add(animal);
                if (animal == VanillaAnimals.BlueChicken && canAlreadyBuyBlueChicken)
                {
                    originalPurchaseAnimal = animal;
                }

                var currentAnimalData = animalData[animal];
                var originalPurchaseAnimalData = originalAnimalData[originalPurchaseAnimal];

                // Update fields using original data
                UpdateAnimalFields(currentAnimalData, originalPurchaseAnimalData);

                if (animal != originalPurchaseAnimal)
                {
                    UpdateUnlockCondition(currentAnimalData, originalPurchaseAnimalData, animal);
                }
            }
        }

        private void UpdateAnimalFields(FarmAnimalData current, FarmAnimalData original)
        {
            current.PurchasePrice = original.PurchasePrice;
            current.RequiredBuilding = original.RequiredBuilding;
            current.ShopDescription = original.ShopDescription;
            current.ShopDisplayName = original.ShopDisplayName;
            current.ShopMissingBuildingDescription = original.ShopMissingBuildingDescription;
            current.ShopSourceRect = original.ShopSourceRect;
            current.ShopTexture = original.ShopTexture;
            current.AlternatePurchaseTypes = original.AlternatePurchaseTypes;
        }

        private void UpdateUnlockCondition(
            FarmAnimalData current,
            FarmAnimalData original,
            string animal
        )
        {
            var alternatePurchaseEntry = original.AlternatePurchaseTypes.Find(ap =>
                ap.AnimalIds.Contains(animal)
            );
            if (alternatePurchaseEntry != null && alternatePurchaseEntry.Condition != null)
            {
                current.UnlockCondition = string.IsNullOrEmpty(original.UnlockCondition)
                    ? alternatePurchaseEntry.Condition
                    : $"{original.UnlockCondition}, {alternatePurchaseEntry.Condition}";
            }
            else
            {
                current.UnlockCondition = original.UnlockCondition;
            }
        }

        private void HandleHiddenAnimal(
            string animal,
            IDictionary<string, FarmAnimalData> animalData,
            string? originalPurchaseAnimal,
            List<(string AnimalKey, string? MasterAnimal)> hiddenAnimals
        )
        {
            animalData[animal].PurchasePrice = -1;

            var clonedAnimalInfo = _letMeHatchApi
                ?.GetClonedAnimalsInfo()
                .Find(x => x.FemaleKey == animal);
            if (clonedAnimalInfo.HasValue)
            {
                var maleKey = clonedAnimalInfo.Value.MaleKey;
                LoggerHelper.VerboseLog(
                    $"Removed {maleKey} from the shop because it's a cloned animal.",
                    LogLevel.Trace,
                    false
                );

                if (
                    clonedAnimalInfo.Value.AddAsAlternatePurchase
                    && animalData[animal].AlternatePurchaseTypes != null
                )
                {
                    animalData[animal]
                        .AlternatePurchaseTypes.RemoveAll(ap => ap.AnimalIds.Contains(maleKey));
                }
                else
                {
                    animalData[animal].PurchasePrice = -1;
                }
            }

            hiddenAnimals.Add((animal, originalPurchaseAnimal));
            if (originalPurchaseAnimal != null)
            {
                animalData[originalPurchaseAnimal]
                    .AlternatePurchaseTypes.ForEach(ap => ap.AnimalIds.Remove(animal));
            }
        }

        private bool ShouldBeAdded(string animalKey)
        {
            return _modSaveData.NewAddedAnimals.Select(nA => nA.FarmAnimalName).Contains(animalKey)
                || !ModEntry.Config.AnimalProgressionMode[animalKey];
        }

        // Optional: Log hidden and available animals if needed
        private void LogAnimalStatus(
            List<(string AnimalKey, string? MasterAnimal)> hiddenAnimals,
            List<string> availableAlternateAnimals
        )
        {
            var hiddenAnimalsString =
                $"Hidden animals: [{string.Join("\n", hiddenAnimals.Select(h => $"{h.AnimalKey}{(h.MasterAnimal != null ? $", alternate purchase of {h.MasterAnimal}" : "")}"))}]";
            var availableAnimalsString =
                $"Available animals: [{string.Join("\n", availableAlternateAnimals.Select(a => $"{a}"))}]";
            LoggerHelper.VerboseLog(hiddenAnimalsString, LogLevel.Trace, false);
            LoggerHelper.VerboseLog(availableAnimalsString, LogLevel.Trace, false);
        }

        /// <summary>Get the translated display name for a farm animal from its data, if any.</summary>
        /// <param name="id">The animal type ID in <c>Data/FarmAnimals</c>.</param>
        /// <param name="forShop">Whether to get the shop name, if applicable.</param>
        internal static string GetDisplayName(string id, bool forShop = false)
        {
            var displayName = forShop
                ? Game1.farmAnimalData[id].ShopDisplayName
                : Game1.farmAnimalData[id].DisplayName;
            if (!displayName.StartsWith('['))
            {
                return displayName;
            }
            else
            {
                return FarmAnimal.GetDisplayName(id, forShop);
            }
        }

        /// <summary>Get the translated shop description for a farm animal from its data, if any.</summary>
        /// <param name="id">The animal type ID in <c>Data/FarmAnimals</c>.</param>
        internal static string GetShopDescription(string id)
        {
            var shopDescription = Game1.farmAnimalData[id].ShopDescription;
            if (shopDescription == null)
            {
                return "";
            }
            if (!shopDescription.StartsWith('['))
            {
                return shopDescription;
            }
            else
            {
                return FarmAnimal.GetShopDescription(id);
            }
        }
    }
}
