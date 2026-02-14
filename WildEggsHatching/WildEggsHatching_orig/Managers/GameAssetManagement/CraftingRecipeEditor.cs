using BirdsOfAFeather.Helpers;
using StardewValley.Extensions;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Enums;
using static WildEggsHatching.EnumsAndStrings.Strings;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.Managers.GameAssetManagement
{
    /// <summary>The mod entry point.</summary>
    internal class CraftingRecipeEditor
    {
        internal readonly Dictionary<string, string> CraftingRecipesToAdd;

        internal CraftingRecipeEditor()
        {
            CraftingRecipesToAdd = new();
            LoggerHelper.VerboseLog("CraftingRecipeEditor initialized.");
        }

        private void CreateCraftingRecipes()
        {
            LoggerHelper.VerboseLog("Creating crafting recipes...");

            // Define ingredients for the Incubator Crafting
            var incubatorIngredients = new List<(string itemID, int quantity)>
            {
                ("390", 80),
                ("118", 20),
                (IDs.Objects.Elixir, 1),
            };
            var incubatorOutput = (IDs.BigCraftables.WildEggIncubator, 1);
            var incubatorRecipe = CreateCraftingRecipeData(
                incubatorIngredients,
                incubatorOutput,
                true,
                null
            );
            CraftingRecipesToAdd[IDs.BigCraftables.WEIncubatorCrafting] = incubatorRecipe;
            LoggerHelper.VerboseLog("Crafting recipe for Wild Egg Incubator created.");

            // Define ingredients for the Elixir Crafting
            var elixirIngredients = new List<(string itemID, int quantity)>
            {
                ("118", 1),
                ("114", 1),
                ("734", 1),
                ("107", 1),
            };
            var elixirOutput = (IDs.Objects.Elixir, 1);
            var elixirRecipe = CreateCraftingRecipeData(
                elixirIngredients,
                elixirOutput,
                false,
                null
            );
            CraftingRecipesToAdd[IDs.Objects.ElixirCrafting] = elixirRecipe;
            LoggerHelper.VerboseLog("Crafting recipe for Elixir created.");
        }

        private string CreateCraftingRecipeData(
            List<(string itemID, int quantity)> ingredients,
            (string outputID, int quantity) output,
            bool isBigCraftable,
            string? unlockConditions
        )
        {
            var ingredientsString = string.Join(
                ' ',
                ingredients.Select(x => $"{x.itemID} {x.quantity}")
            );
            var outputString = $"{output.outputID} {output.quantity}";
            var unlockConditionsString = string.IsNullOrEmpty(unlockConditions)
                ? "none"
                : unlockConditions;

            LoggerHelper.VerboseLog(
                $"Creating crafting recipe data: Ingredients: {ingredientsString}, Output: {outputString}, IsBigCraftable: {isBigCraftable}, UnlockConditions: {unlockConditionsString}"
            );

            return $"{ingredientsString}//{outputString}/{isBigCraftable}/{unlockConditionsString}/";
        }

        internal void AddCraftingRecipes(IDictionary<string, string> data)
        {
            LoggerHelper.VerboseLog("Adding crafting recipes...");
            GameAssetManager.PopulateIfEmpty(CraftingRecipesToAdd, () => CreateCraftingRecipes());
            data.TryAddMany(CraftingRecipesToAdd);
            LoggerHelper.VerboseLog("Crafting recipes added.");
        }

        internal void EditCraftingRecipesAsset()
        {
            LoggerHelper.VerboseLog("Editing crafting recipes asset...");

            if (!ModEntry.Config.ProgressionMode)
            {
                LoggerHelper.VerboseLog(
                    "Mod is not in Progression Mode. Modifying Wild Egg Incubator crafting recipe."
                );
                CraftingRecipesToAdd[IDs.BigCraftables.WEIncubatorCrafting] =
                    DataFieldsEditor.ModifyData(
                        CraftingRecipesToAdd[IDs.BigCraftables.WEIncubatorCrafting],
                        4,
                        "default"
                    );
                CraftingRecipesToAdd[IDs.Objects.ElixirCrafting] = DataFieldsEditor.ModifyData(
                    CraftingRecipesToAdd[IDs.Objects.ElixirCrafting],
                    4,
                    "default"
                );
            }
            if (!ModEntry.Config.NeedsFern)
            {
                LoggerHelper.VerboseLog(
                    "Mod does not need fern. Modifying Wild Egg Incubator crafting recipe to include item 259."
                );
                CraftingRecipesToAdd[IDs.BigCraftables.WEIncubatorCrafting] =
                    DataFieldsEditor.ModifyData(
                        CraftingRecipesToAdd[IDs.BigCraftables.WEIncubatorCrafting],
                        0,
                        ListEditType.Add,
                        ' ',
                        ("259", "3")
                    );
            }
            if (ModEntry.Config.FruitInsteadOfSeed)
            {
                LoggerHelper.VerboseLog(
                    "Mod uses fruit instead of seed. Modifying Elixir crafting recipe to use fruit."
                );
                CraftingRecipesToAdd[IDs.Objects.ElixirCrafting] = CraftingRecipesToAdd[
                    IDs.Objects.ElixirCrafting
                ]
                    .Replace("114 1", "454 1");
            }
            LoggerHelper.VerboseLog("Crafting recipes asset edited successfully.");
        }
    }
}
