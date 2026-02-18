using System.Text.RegularExpressions;
using ContentPatcher;
using LetMeHatchThisEgg.Models;
using LetMeHatchThisEgg.Core.Cloning;
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
    public class AnimalCloner
    {
        //Dependencies
        private readonly List<ClonedAnimalInfo> ClonedAnimalsInfoList;
        private readonly List<IContentPack> LoadedContentPacks;
        private readonly IContentPatcherAPI ContentPatcherAPI;

        private readonly CloneMappingService _cloneMappingService;

        public AnimalCloner(
            IContentPatcherAPI contentPatcherAPI,
            List<ClonedAnimalInfo> clonedAnimalInfos,
            List<IContentPack> loadedContentPacks
        )
        {
            ContentPatcherAPI = contentPatcherAPI;
            ClonedAnimalsInfoList = clonedAnimalInfos;
            LoadedContentPacks = loadedContentPacks;
            _cloneMappingService = new CloneMappingService();
        }

        // private void CreateAndAddAnimalClonesDebug(
        //     AssetRequestedEventArgs e,
        //     AssetEditPriority priority = AssetEditPriority.Late
        // )
        // {
        //     e.Edit(
        //         asset =>
        //         {
        //             var data = asset.AsDictionary<string, FarmAnimalData>().Data;

        //             foreach (var clonedAnimalInfo in ClonedAnimalsInfoList)
        //             {
        //                 var femaleKey = clonedAnimalInfo.FemaleKey;
        //                 var maleKey = clonedAnimalInfo.MaleKey;
        //                 var contentPack = LoadedContentPacks.First(cp =>
        //                     cp.Manifest.UniqueID == clonedAnimalInfo.ContentPackID
        //                 );
        //                 EditClonedAnimal(contentPack, maleKey, data[femaleKey], data[maleKey]);
        //                 var clonedAnimalData = data[maleKey];
        //                 data.Remove(maleKey);
        //                 var dataAsList = data.ToList();
        //                 var femaleKvp = dataAsList.Find(x => x.Key == femaleKey);
        //                 // Find the index of the element after which you want to insert the new element
        //                 int index = dataAsList.IndexOf(femaleKvp);
        //                 // Insert the new element after "egg2"

        //                 dataAsList.Insert(index + 1, new(maleKey, clonedAnimalData));
        //                 asset
        //                     .AsDictionary<string, FarmAnimalData>()
        //                     .ReplaceWith(dataAsList.ToDictionary(x => x.Key, x => x.Value));
        //                 data = asset.AsDictionary<string, FarmAnimalData>().Data;
        //                 if (
        //                     femaleKey == VanillaAnimals.VoidChicken
        //                     && data[VanillaAnimals.VoidChicken].PurchasePrice == -1
        //                 )
        //                 {
        //                     data[VanillaAnimals.WhiteChicken].AlternatePurchaseTypes ??= new();
        //                     data[VanillaAnimals.WhiteChicken]
        //                         .AlternatePurchaseTypes.Add(
        //                             new()
        //                             {
        //                                 AnimalIds = new() { maleKey, femaleKey },
        //                                 Id = maleKey
        //                             }
        //                         );
        //                 }
        //                 else if (
        //                     clonedAnimalInfo.AddAsAlternatePurchase
        //                     || (
        //                         !clonedAnimalInfo.AddAsAlternatePurchase
        //                         && data[femaleKey].PurchasePrice == -1
        //                     )
        //                 )
        //                 {
        //                     data[maleKey].PurchasePrice = -1;
        //                     var originalPurchaseAnimal = data.GetOriginalPurchaseAnimal(femaleKey);

        //                     FarmAnimalData targetShopAnimalEntry;
        //                     if (
        //                         originalPurchaseAnimal != null
        //                         && originalPurchaseAnimal.Value.Key != null
        //                     )
        //                     {
        //                         targetShopAnimalEntry = originalPurchaseAnimal.Value.Value;
        //                     }
        //                     else
        //                     {
        //                         targetShopAnimalEntry = data[femaleKey];
        //                     }
        //                     targetShopAnimalEntry.AlternatePurchaseTypes ??= new();
        //                     if (
        //                         targetShopAnimalEntry.AlternatePurchaseTypes.Any(x =>
        //                             x.Condition == null
        //                         )
        //                     )
        //                     {
        //                         targetShopAnimalEntry
        //                             .AlternatePurchaseTypes.First(x => x.Condition == null)
        //                             .AnimalIds.Add(maleKey);
        //                     }
        //                     else
        //                     {
        //                         targetShopAnimalEntry.AlternatePurchaseTypes.Add(
        //                             new()
        //                             {
        //                                 AnimalIds = new() { maleKey },
        //                                 Id = maleKey
        //                             }
        //                         );
        //                     }
        //                 }
        //             }
        //         },
        //         priority
        //     );

        //     LoggerHelper.VerboseLog("CreateAndAddAnimalClones method finished.", LogLevel.Info);
        // }

        public void EditAnimalData(IAssetData asset)
        {
            var data = asset.AsDictionary<string, FarmAnimalData>().Data;
            foreach (var clonedAnimalInfo in ClonedAnimalsInfoList)
            {
                var femaleKey = clonedAnimalInfo.FemaleKey;
                var maleKey = clonedAnimalInfo.MaleKey;
                var contentPack = GetContentPack(clonedAnimalInfo.ContentPackID);
                var clonedAnimalData = data[maleKey];
                EditClonedAnimal(contentPack, maleKey, data[femaleKey], clonedAnimalData);
                _cloneMappingService.ApplyAlternatePurchaseRules(clonedAnimalInfo, data);
                //InsertClonedAnimalData(asset, data, femaleKey, maleKey, clonedAnimalData);
            }
        }

        // Extracted methods for better readability and maintainability
        private IContentPack GetContentPack(string contentPackID)
        {
            return LoadedContentPacks.First(cp => cp.Manifest.UniqueID == contentPackID);
        }

        private void InsertClonedAnimalData(
            IAssetData asset,
            IDictionary<string, FarmAnimalData> data,
            string femaleKey,
            string maleKey,
            FarmAnimalData clonedAnimalData
        )
        {
            data.Remove(maleKey);
            var dataAsList = data.ToList();
            var femaleKvp = dataAsList.Find(x => x.Key == femaleKey);
            int index = dataAsList.IndexOf(femaleKvp);
            dataAsList.Insert(index + 1, new(maleKey, clonedAnimalData));
            asset.ReplaceWith(dataAsList.ToDictionary(x => x.Key, x => x.Value));
        }

        public void EditClonedAnimal(
            IContentPack contentPack,
            string maleKey,
            FarmAnimalData originalAnimalData,
            FarmAnimalData currentCloneData
        )
        {
            var placeholderData = GetFarmAnimalDataPlaceholder();
            currentCloneData.Gender = FarmAnimalGender.Male;
            currentCloneData.HarvestType = FarmAnimalHarvestType.DropOvernight;

            SetTextureIfNeeded(
                contentPack,
                currentCloneData,
                originalAnimalData,
                placeholderData,
                maleKey
            );
            SetIfValueIsPlaceholder(
                contentPack,
                ref currentCloneData.DisplayName,
                originalAnimalData.DisplayName,
                placeholderData.DisplayName
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.House,
                originalAnimalData.House,
                placeholderData.House
            );

            SetIfValueIsPlaceholder(
                contentPack,
                ref currentCloneData.ShopDisplayName,
                originalAnimalData.ShopDisplayName,
                placeholderData.ShopDisplayName
            );
            SetIfValueIsPlaceholder(
                contentPack,
                ref currentCloneData.ShopDescription,
                originalAnimalData.ShopDescription,
                placeholderData.ShopDescription
            );
            SetIfValueIsPlaceholder(
                contentPack,
                ref currentCloneData.ShopMissingBuildingDescription,
                originalAnimalData.ShopMissingBuildingDescription,
                placeholderData.ShopMissingBuildingDescription
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.RequiredBuilding,
                originalAnimalData.RequiredBuilding,
                placeholderData.RequiredBuilding
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.UnlockCondition,
                originalAnimalData.UnlockCondition,
                placeholderData.UnlockCondition
            );
            SetIfValueIsPlaceholder(
                contentPack,
                ref currentCloneData.BirthText,
                originalAnimalData.BirthText,
                placeholderData.BirthText
            );

            SetIfValueIsPlaceholder(
                ref currentCloneData.HarvestTool,
                originalAnimalData.HarvestTool,
                placeholderData.HarvestTool
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.Sound,
                originalAnimalData.Sound,
                placeholderData.Sound
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.BabySound,
                originalAnimalData.BabySound,
                placeholderData.BabySound
            );

            currentCloneData.ShadowWhenBabySwims ??= originalAnimalData.ShadowWhenBabySwims;
            currentCloneData.ShadowWhenBaby ??= originalAnimalData.ShadowWhenBaby;
            currentCloneData.ShadowWhenAdultSwims ??= originalAnimalData.ShadowWhenAdultSwims;
            currentCloneData.ShadowWhenAdult ??= originalAnimalData.ShadowWhenAdult;
            currentCloneData.EggItemIds = null;

            currentCloneData.Skins ??=
                originalAnimalData.Skins != null ? new(originalAnimalData.Skins) : new();
            currentCloneData.AlternatePurchaseTypes = null;
            currentCloneData.StatToIncrementOnProduce ??=
                originalAnimalData.StatToIncrementOnProduce != null
                    ? new(originalAnimalData.StatToIncrementOnProduce)
                    : new();

            currentCloneData.CustomFields =
                originalAnimalData.CustomFields != null
                    ? new(originalAnimalData.CustomFields)
                    : new();

            SetIfValueIsPlaceholder(
                ref currentCloneData.PurchasePrice,
                originalAnimalData.PurchasePrice,
                placeholderData.PurchasePrice
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.SellPrice,
                originalAnimalData.SellPrice,
                placeholderData.SellPrice
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.IncubationTime,
                originalAnimalData.IncubationTime,
                placeholderData.IncubationTime
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.IncubatorParentSheetOffset,
                originalAnimalData.IncubatorParentSheetOffset,
                placeholderData.IncubatorParentSheetOffset
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.DaysToMature,
                originalAnimalData.DaysToMature,
                placeholderData.DaysToMature
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.DaysToProduce,
                originalAnimalData.DaysToProduce,
                placeholderData.DaysToProduce
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.FriendshipForFasterProduce,
                originalAnimalData.FriendshipForFasterProduce,
                placeholderData.FriendshipForFasterProduce
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.DeluxeProduceMinimumFriendship,
                originalAnimalData.DeluxeProduceMinimumFriendship,
                placeholderData.DeluxeProduceMinimumFriendship
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.DeluxeProduceCareDivisor,
                originalAnimalData.DeluxeProduceCareDivisor,
                placeholderData.DeluxeProduceCareDivisor
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.DeluxeProduceLuckMultiplier,
                originalAnimalData.DeluxeProduceLuckMultiplier,
                placeholderData.DeluxeProduceLuckMultiplier
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.ProfessionForHappinessBoost,
                originalAnimalData.ProfessionForHappinessBoost,
                placeholderData.ProfessionForHappinessBoost
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.ProfessionForQualityBoost,
                originalAnimalData.ProfessionForQualityBoost,
                placeholderData.ProfessionForQualityBoost
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.ProfessionForFasterProduce,
                originalAnimalData.ProfessionForFasterProduce,
                placeholderData.ProfessionForFasterProduce
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.SpriteWidth,
                originalAnimalData.SpriteWidth,
                placeholderData.SpriteWidth
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.SpriteHeight,
                originalAnimalData.SpriteHeight,
                placeholderData.SpriteHeight
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.SleepFrame,
                originalAnimalData.SleepFrame,
                placeholderData.SleepFrame
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.GrassEatAmount,
                originalAnimalData.GrassEatAmount,
                placeholderData.GrassEatAmount
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.HappinessDrain,
                originalAnimalData.HappinessDrain,
                placeholderData.HappinessDrain
            );

            SetIfValueIsPlaceholder(
                ref currentCloneData.ShopSourceRect,
                originalAnimalData.ShopSourceRect,
                placeholderData.ShopSourceRect
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.UpDownPetHitboxTileSize,
                originalAnimalData.UpDownPetHitboxTileSize,
                placeholderData.UpDownPetHitboxTileSize
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.LeftRightPetHitboxTileSize,
                originalAnimalData.LeftRightPetHitboxTileSize,
                placeholderData.LeftRightPetHitboxTileSize
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.BabyUpDownPetHitboxTileSize,
                originalAnimalData.BabyUpDownPetHitboxTileSize,
                placeholderData.BabyUpDownPetHitboxTileSize
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.BabyLeftRightPetHitboxTileSize,
                originalAnimalData.BabyLeftRightPetHitboxTileSize,
                placeholderData.BabyLeftRightPetHitboxTileSize
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.EmoteOffset,
                originalAnimalData.EmoteOffset,
                placeholderData.EmoteOffset
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.SwimOffset,
                originalAnimalData.SwimOffset,
                placeholderData.SwimOffset
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.ProduceOnMature,
                originalAnimalData.ProduceOnMature,
                placeholderData.ProduceOnMature
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.CanEatGoldenCrackers,
                originalAnimalData.CanEatGoldenCrackers,
                placeholderData.CanEatGoldenCrackers
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.UseFlippedRightForLeft,
                originalAnimalData.UseFlippedRightForLeft,
                placeholderData.UseFlippedRightForLeft
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.UseDoubleUniqueAnimationFrames,
                originalAnimalData.UseDoubleUniqueAnimationFrames,
                placeholderData.UseDoubleUniqueAnimationFrames
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.CanSwim,
                originalAnimalData.CanSwim,
                placeholderData.CanSwim
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.BabiesFollowAdults,
                originalAnimalData.BabiesFollowAdults,
                placeholderData.BabiesFollowAdults
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.ShowInSummitCredits,
                originalAnimalData.ShowInSummitCredits,
                placeholderData.ShowInSummitCredits
            );
            SetIfValueIsPlaceholder(
                ref currentCloneData.CanGetPregnant,
                originalAnimalData.CanGetPregnant,
                placeholderData.CanGetPregnant
            );
            currentCloneData.EggItemIds ??= new();

            var pattern = @".*egg.*";

            if (currentCloneData.ProduceItemIds.Count == 0)
            {
                currentCloneData.ProduceItemIds =
                    originalAnimalData.ProduceItemIds != null
                        ? new List<FarmAnimalProduce>(originalAnimalData.ProduceItemIds)
                        : new();

                currentCloneData.ProduceItemIds.RemoveAll(e =>
                    AnimalProducts.VanillaEggs.Contains(e.ItemId)
                    || Regex.IsMatch(e.ItemId, pattern)
                    || (
                        Game1.objectData.ContainsKey(e.ItemId)
                        && Game1.objectData[e.ItemId].Category == Object.EggCategory
                    )
                );
            }
            if (currentCloneData.DeluxeProduceItemIds.Count == 0)
            {
                currentCloneData.DeluxeProduceItemIds =
                    originalAnimalData.DeluxeProduceItemIds != null
                        ? new List<FarmAnimalProduce>(originalAnimalData.DeluxeProduceItemIds)
                        : new();

                currentCloneData.DeluxeProduceItemIds.RemoveAll(e =>
                    AnimalProducts.VanillaEggs.Contains(e.ItemId)
                    || Regex.IsMatch(e.ItemId, pattern)
                    || (
                        Game1.objectData.ContainsKey(e.ItemId)
                        && Game1.objectData[e.ItemId].Category == Object.EggCategory
                    )
                );
            }
        }

        private void SetIfValueIsPlaceholder<T>(
            ref T currentValue,
            T originalValue,
            T predeterminedValue
        )
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, predeterminedValue))
            {
                currentValue = originalValue;
            }
        }

        private void SetIfValueIsPlaceholder(
            ref string currentValue,
            string originalValue,
            string predeterminedValue
        )
        {
            var tokenString = ContentPatcherAPI.ParseTokenString(
                manifest: ModEntry.ModManifestInstance,
                rawValue: currentValue,
                formatVersion: new SemanticVersion("2.3.0")
            );
            tokenString.UpdateContext();
            var currentValueTokenString = tokenString.Value ?? currentValue;
            if (currentValue == predeterminedValue)
            {
                currentValue = originalValue;
            }
            else
            {
                currentValue = currentValueTokenString;
            }
        }

        private void SetIfValueIsPlaceholder(
            IContentPack contentPack,
            ref string currentValue,
            string originalValue,
            string predeterminedValue
        )
        {
            string pattern = @"{{i18n:(.*?)}}";
            if (currentValue == predeterminedValue)
            {
                LoggerHelper.VerboseLog($"Setting {currentValue} to  {originalValue}");
                currentValue = originalValue;
            }
            else if (Regex.Match(currentValue, pattern).Success)
            {
                Match match = Regex.Match(currentValue, pattern);
                string key = match.Groups[1].Value.Replace(" ", "");
                currentValue = contentPack.Translation.Get(key);
            }
        }

        private void SetTextureIfNeeded(
            IContentPack contentPack,
            FarmAnimalData currentData,
            FarmAnimalData originalData,
            FarmAnimalData placeholderData,
            string animalKey
        )
        {
            var contentPackID = contentPack.Manifest.UniqueID;
            var cloneInfo = ClonedAnimalsInfoList.Find(a => a.MaleKey == animalKey);
            if (cloneInfo == null)
            {
                return;
            }
            currentData.BabyTexture = GetTextureString(
                currentData.BabyTexture,
                originalData.BabyTexture,
                placeholderData.BabyTexture,
                contentPackID,
                animalKey,
                TextureType.Baby
            );
            currentData.Texture = GetTextureString(
                currentData.Texture,
                originalData.Texture,
                placeholderData.Texture,
                contentPackID,
                animalKey,
                TextureType.Adult
            );
            currentData.HarvestedTexture = GetTextureString(
                currentData.HarvestedTexture,
                originalData.HarvestedTexture,
                placeholderData.HarvestedTexture,
                contentPackID,
                animalKey,
                TextureType.Harvested
            );
            currentData.ShopTexture = GetTextureString(
                currentData.ShopTexture,
                originalData.ShopTexture,
                placeholderData.ShopTexture,
                contentPackID,
                animalKey,
                TextureType.Shop
            );
        }

        private string GetTextureString(
            string? currentValue,
            string originalValue,
            string? placeholderValue,
            string contentPackID,
            string animalKey,
            TextureType textureName
        )
        {
            var clonedInfo = ClonedAnimalsInfoList.Find(x => x.MaleKey == animalKey);
            string texture;
            if (currentValue == null)
            {
                texture = originalValue;
            }
            else if (currentValue.Equals("cp", StringComparison.OrdinalIgnoreCase))
            {
                texture =
                    $"Animals/MaleClones/{contentPackID}/{clonedInfo?.FemaleKey}/{textureName}";
                clonedInfo!.Textures[textureName] = texture;
            }
            else
            {
                texture = (string)currentValue.Clone();
            }
            return texture;
        }

        internal static FarmAnimalData GetFarmAnimalDataPlaceholder()
        {
            return new FarmAnimalData
            {
                AlternatePurchaseTypes = null,
                EggItemIds = null,
                ProduceItemIds = new(),
                DeluxeProduceItemIds = new(),
                Skins = null,
                StatToIncrementOnProduce = null,
                CustomFields = null
            };
        }
    }
}
