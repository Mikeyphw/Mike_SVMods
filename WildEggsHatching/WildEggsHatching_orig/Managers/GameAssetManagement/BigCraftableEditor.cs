using Microsoft.Xna.Framework;
using StardewModdingAPI;
using Mikew.BirdsOfAFeather.Helpers;
using StardewValley.GameData.BigCraftables;
using static Mikew.BirdsOfAFeather.Helpers.ModAssetHandler;
using static WildEggsHatching.EnumsAndStrings.Enums.Assets;
using static WildEggsHatching.EnumsAndStrings.Strings;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.Managers.GameAssetManagement
{
    /// <summary>The mod entry point.</summary>
    internal class BigCraftableEditor
    {
        internal readonly Dictionary<string, BigCraftableData> BigCraftablesToAdd;

        internal BigCraftableEditor()
        {
            BigCraftablesToAdd = new();
            LoggerHelper.VerboseLog("BigCraftableEditor initialized.");
        }

        private void CreateBigCraftables()
        {
            LoggerHelper.VerboseLog("Creating big craftables...");
            BigCraftablesToAdd[IDs.BigCraftables.WildEggIncubator] = CreateWildEggIncubator();
            LoggerHelper.VerboseLog("Big craftable WildEggIncubator created.");
        }

        internal void AddBigCraftables(IDictionary<string, BigCraftableData> data)
        {
            LoggerHelper.VerboseLog("Adding big craftables...");
            GameAssetManager.PopulateIfEmpty(BigCraftablesToAdd, () => CreateBigCraftables());
            data.TryAddMany(BigCraftablesToAdd);
            LoggerHelper.VerboseLog("Big craftables added.");
        }

        private BigCraftableData CreateWildEggIncubator()
        {
            LoggerHelper.VerboseLog("Creating WildEggIncubator BigCraftableData...");
            var incubatorData = new BigCraftableData()
            {
                Name = IDs.BigCraftables.WildEggIncubator,
                DisplayName = TranslationHelper.Get("object.WildEggIncubator.name"),
                Description = TranslationHelper.Get("object.WildEggIncubator.description"),
                Texture = AssetHandler.GetAssetPath(
                    BigCraftable.WildEggIncubator,
                    AssetPathType.GamePath
                ),
                SpriteIndex = 0,
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = false,
            };
            LoggerHelper.VerboseLog("WildEggIncubator BigCraftableData created.");
            return incubatorData;
        }

        internal void EditWildEggIncubatorAsset(IAssetDataForImage editor)
        {
            LoggerHelper.VerboseLog("Editing WildEggIncubator asset...");
            IRawTextureData sourceImage = ModEntry.HelperInstance.ModContent.Load<IRawTextureData>(
                AssetHandler.GetAssetPath(
                    BigCraftable.WildEggIncubator,
                    AssetPathType.ModFolderPath
                )
            );
            editor.PatchImage(
                sourceImage,
                sourceArea: new Rectangle(0, 32, 16, 32),
                targetArea: new Rectangle(0, 0, 16, 32)
            );
            LoggerHelper.VerboseLog("WildEggIncubator asset edited successfully.");
        }
    }
}
