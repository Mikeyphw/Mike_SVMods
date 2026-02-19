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
    public class AnimalCloner : IAnimalCloner
    {
        // Dependencies
        private readonly IContentStringEvaluator _stringEvaluator;
        private readonly ICloneTextureBookkeeper _cloneTextureBookkeeper;
        private readonly FarmAnimalCloneTransformer _farmAnimalCloneTransformer;

        public AnimalCloner(
            IContentStringEvaluator stringEvaluator,
            ICloneTextureBookkeeper cloneTextureBookkeeper
        )
        {
            _stringEvaluator = stringEvaluator;
            _cloneTextureBookkeeper = cloneTextureBookkeeper;
            _farmAnimalCloneTransformer = new FarmAnimalCloneTransformer();
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
                contentPackId: contentPack.Manifest.UniqueID,
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
