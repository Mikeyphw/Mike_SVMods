using LetMeHatchThisEgg.Models;
using LetMeHatchThisEgg.Core.Cloning;
using LetMeHatchThisEgg.Core.Placeholders;
using LetMeHatchThisEgg.Infrastructure.Cloning;
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
        private readonly IContentStringEvaluator _stringEvaluator;
        private readonly CloneMappingService _cloneMappingService;
        private readonly ICloneTextureBookkeeper _cloneTextureBookkeeper;

        private readonly FarmAnimalCloneTransformer _farmAnimalCloneTransformer;

        public AnimalCloner(
            IContentStringEvaluator stringEvaluator,
            ICloneTextureBookkeeper cloneTextureBookkeeper,
            List<ClonedAnimalInfo> clonedAnimalInfos,
            List<IContentPack> loadedContentPacks
        )
        {
            _stringEvaluator = stringEvaluator;
            _cloneTextureBookkeeper = cloneTextureBookkeeper;
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
                EditClonedAnimal(contentPack, clonedAnimalInfo, data[femaleKey], clonedAnimalData);
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
            ClonedAnimalInfo cloneInfo,
            FarmAnimalData originalAnimalData,
            FarmAnimalData currentCloneData
        )
        {
            var placeholderData = GetFarmAnimalDataPlaceholder();
            _farmAnimalCloneTransformer.ApplyPreLocalization(currentCloneData);

            _cloneTextureBookkeeper.ApplyTextures(
                contentPack: contentPack,
                cloneInfo: cloneInfo,
                cloneData: currentCloneData,
                originalData: originalAnimalData,
                placeholderData: placeholderData
            );

            ApplyTextFieldPolicy(
                evaluator: _stringEvaluator,
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
            IContentStringEvaluator evaluator,
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
                            evaluator,
                            contentPack,
                            ref clone.DisplayName,
                            original.DisplayName,
                            decision.Decision
                        );
                        break;

                    case FarmAnimalTextField.ShopDisplayName:
                        ApplyTextDecision(
                            evaluator,
                            contentPack,
                            ref clone.ShopDisplayName,
                            original.ShopDisplayName,
                            decision.Decision
                        );
                        break;

                    case FarmAnimalTextField.ShopDescription:
                        ApplyTextDecision(
                            evaluator,
                            contentPack,
                            ref clone.ShopDescription,
                            original.ShopDescription,
                            decision.Decision
                        );
                        break;

                    case FarmAnimalTextField.ShopMissingBuildingDescription:
                        ApplyTextDecision(
                            evaluator,
                            contentPack,
                            ref clone.ShopMissingBuildingDescription,
                            original.ShopMissingBuildingDescription,
                            decision.Decision
                        );
                        break;

                    case FarmAnimalTextField.BirthText:
                        ApplyTextDecision(
                            evaluator,
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
            IContentStringEvaluator evaluator,
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
                currentValue = evaluator.TranslateAndEvaluateTokens(contentPack, decision.I18nKey);
            }
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