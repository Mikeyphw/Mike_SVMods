using ContentPatcher;
using LetMeHatchThisEgg.Models;
using LetMeHatchThisEgg.Core.Cloning;
using LetMeHatchThisEgg.Core.Placeholders;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings.ItemIds;
using Object = StardewValley.Object;

using static LetMeHatchThisEgg.ModEntry;

namespace LetMeHatchThisEgg.Cloning
{
    public class AnimalCloner
    {
        //Dependencies
        private readonly List<ClonedAnimalInfo> ClonedAnimalsInfoList;
        private readonly List<IContentPack> LoadedContentPacks;
        private readonly IContentPatcherAPI ContentPatcherAPI;

        private readonly CloneMappingService _cloneMappingService;

        private readonly FarmAnimalCloneTransformer _farmAnimalCloneTransformer;

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
            _farmAnimalCloneTransformer = new FarmAnimalCloneTransformer();
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
            _farmAnimalCloneTransformer.ApplyPreLocalization(currentCloneData);

            SetTextureIfNeeded(
                contentPack,
                currentCloneData,
                originalAnimalData,
                placeholderData,
                maleKey
            );

            ApplyTextFieldPolicy(
                contentPack: contentPack,
                clone: currentCloneData,
                original: originalAnimalData,
                placeholder: placeholderData
            );
            FarmAnimalCloneNonLocalizedFieldPolicy.Apply(
                clone: currentCloneData,
                original: originalAnimalData,
                placeholder: placeholderData
            );

            _farmAnimalCloneTransformer.ApplyPostLocalization(
                clone: currentCloneData,
                original: originalAnimalData,
                isEggCategoryObjectId: IsEggCategoryObjectId
            );
        }

        private static void ApplyTextFieldPolicy(
            IContentPack contentPack,
            FarmAnimalData clone,
            FarmAnimalData original,
            FarmAnimalData placeholder
        )
        {
            var decisions = FarmAnimalCloneTextFieldPolicy.Evaluate(
                current: new FarmAnimalTextFields(
                    DisplayName: clone.DisplayName,
                    ShopDisplayName: clone.ShopDisplayName,
                    ShopDescription: clone.ShopDescription,
                    ShopMissingBuildingDescription: clone.ShopMissingBuildingDescription,
                    BirthText: clone.BirthText
                ),
                placeholder: new FarmAnimalTextFields(
                    DisplayName: placeholder.DisplayName,
                    ShopDisplayName: placeholder.ShopDisplayName,
                    ShopDescription: placeholder.ShopDescription,
                    ShopMissingBuildingDescription: placeholder.ShopMissingBuildingDescription,
                    BirthText: placeholder.BirthText
                )
            );

            foreach (var decision in decisions)
            {
                switch (decision.Field)
                {
                    case FarmAnimalTextField.DisplayName:
                        ApplyTextDecision(
                            contentPack,
                            ref clone.DisplayName,
                            original.DisplayName,
                            decision.Decision
                        );
                        break;

                    case FarmAnimalTextField.ShopDisplayName:
                        ApplyTextDecision(
                            contentPack,
                            ref clone.ShopDisplayName,
                            original.ShopDisplayName,
                            decision.Decision
                        );
                        break;

                    case FarmAnimalTextField.ShopDescription:
                        ApplyTextDecision(
                            contentPack,
                            ref clone.ShopDescription,
                            original.ShopDescription,
                            decision.Decision
                        );
                        break;

                    case FarmAnimalTextField.ShopMissingBuildingDescription:
                        ApplyTextDecision(
                            contentPack,
                            ref clone.ShopMissingBuildingDescription,
                            original.ShopMissingBuildingDescription,
                            decision.Decision
                        );
                        break;

                    case FarmAnimalTextField.BirthText:
                        ApplyTextDecision(
                            contentPack,
                            ref clone.BirthText,
                            original.BirthText,
                            decision.Decision
                        );
                        break;
                }
            }
        }

        private static void ApplyTextDecision(
            IContentPack contentPack,
            ref string currentValue,
            string originalValue,
            PlaceholderStringDecision decision
        )
        {
            if (decision.UseOriginal)
            {
                currentValue = originalValue;
                return;
            }

            if (decision.I18nKey != null)
            {
                currentValue = contentPack.Translation.Get(decision.I18nKey);
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
            if (clonedInfo == null)
            {
                return originalValue;
            }

            var result = CloneTextureResolver.Resolve(
                currentValue: currentValue,
                originalValue: originalValue,
                contentPackId: contentPackID,
                femaleKey: clonedInfo.FemaleKey,
                textureType: textureName
            );

            if (result.IsContentPackAsset)
            {
                clonedInfo.Textures[textureName] = result.Value;
            }

            return result.Value;
        }

        private static bool IsEggCategoryObjectId(string itemId)
        {
            return Game1.objectData.TryGetValue(itemId, out var obj) && obj.Category == Object.EggCategory;
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
