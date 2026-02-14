using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Events;
using StardewModdingAPI;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Content;

namespace WildEggsHatching.Infrastructure.Content.Editors;

/// <summary>
/// Editor for Data/CraftingRecipes asset - adds wild egg recipes.
/// Refactored to use factory methods and support runtime config changes.
/// </summary>
public sealed class CraftingRecipesEditor : ProgressionAwareAssetEditor
{
    /// <inheritdoc />
    public override string AssetName => GameAssets.CraftingRecipes;

    /// <summary>
    /// Creates a new crafting recipes editor.
    /// </summary>
    public CraftingRecipesEditor() : base() { }

    /// <summary>
    /// Creates a new crafting recipes editor with progression service.
    /// </summary>
    public CraftingRecipesEditor(
        PhaseProgressionService progressionService,
        IConfigProvider configProvider,
        IMonitor monitor,
        IModHelper modHelper) 
        : base(progressionService, configProvider, monitor, modHelper) { }

    /// <inheritdoc />
    public override void Edit<T>(IAssetData<T> asset, string assetName)
    {
        if (assetName != GameAssets.CraftingRecipes)
            return;

        var recipes = asset.AsDictionary<string, string>().Data;

        // Part V: Wild Egg Incubator Recipe
        if (HasReachedStep(ProgressionStep.PartV))
        {
            AddWildEggIncubatorRecipe(recipes);
            AddElixirRecipe(recipes);
        }

        Log($"CraftingRecipesEditor applied edits to {recipes.Count} recipes");
    }

    /// <summary>
    /// Adds the wild egg incubator recipe.
    /// </summary>
    private void AddWildEggIncubatorRecipe(IDictionary<string, string> recipes)
    {
        const string recipeId = "Wild Egg Incubator";
        
        if (recipes.ContainsKey(recipeId))
            return;

        // Get config values
        var config = GetConfig();
        
        // Build ingredients based on config
        var ingredients = new List<(string itemId, int quantity)>
        {
            (WildEggConstants.VanillaItems.Stone, 80),
            (WildEggConstants.VanillaItems.GoldBar, 20)
        };

        // Add iridium bars based on progression (default: 5)
        int iridiumCount = 5;
        if (config.ProgressionMode)
        {
            // In progression mode, may have different requirements
            iridiumCount = 5;
        }
        ingredients.Add((WildEggConstants.VanillaItems.IridiumBar, iridiumCount));

        // Optionally add Crystalarium
        if (!config.NeedsFern)
        {
            ingredients.Add((WildEggConstants.VanillaItems.Crystalarium, 1));
        }

        // Create recipe string using factory method
        var description = Translate("recipe.wildEggIncubator.description", "Enables crafting of the Wild Egg Incubator");
        recipes[recipeId] = CreateRecipeString(
            ingredients,
            (WildEggConstants.Items.WildEggIncubator, 1),
            isBigCraftable: true,
            unlockConditions: null,
            description: description
        );
    }

    /// <summary>
    /// Adds the Elixir recipe.
    /// </summary>
    private void AddElixirRecipe(IDictionary<string, string> recipes)
    {
        const string recipeId = "Elixir Of Revitalized Essence";
        
        if (recipes.ContainsKey(recipeId))
            return;

        var config = GetConfig();

        // Default ingredients
        var ingredients = new List<(string itemId, int quantity)>
        {
            ("118", 1), // Wine
            ("734", 1), // Pickles
        };

        // Check for fruit instead of seed config
        if (config.FruitInsteadOfSeed)
        {
            ingredients.Add(("454", 1)); // Fruit
        }
        else
        {
            ingredients.Add(("114", 1)); // Seeds
        }

        ingredients.Add(("107", 1)); // Life Elixir

        recipes[recipeId] = CreateRecipeString(
            ingredients,
            (WildEggConstants.Items.Elixir, 1),
            isBigCraftable: false,
            unlockConditions: null,
            description: Translate("recipe.elixir.description", "Craft a powerful elixir")
        );
    }

    /// <summary>
    /// Edits crafting recipes based on runtime config changes.
    /// Call this when config changes.
    /// </summary>
    public void EditCraftingRecipesAsset(IDictionary<string, string> recipes)
    {
        var config = GetConfig();

        // Modify Wild Egg Incubator recipe based on config
        if (recipes.TryGetValue("Wild Egg Incubator", out var incubatorRecipe))
        {
            // If not in progression mode, change to "default" unlock
            if (!config.ProgressionMode)
            {
                var modified = ModifyRecipeField(incubatorRecipe, 4, "default");
                if (modified != null)
                {
                    recipes["Wild Egg Incubator"] = modified;
                }
            }

            // If doesn't need fern, remove it from ingredients
            if (!config.NeedsFern)
            {
                // Replace "259 3" (fern) with nothing or add more stone
                // This is a simple string replace approach
                recipes["Wild Egg Incubator"] = incubatorRecipe.Replace("259 3", "");
            }
        }

        // Modify Elixir recipe based on config
        if (recipes.TryGetValue("Elixir Of Revitalized Essence", out var elixirRecipe))
        {
            if (config.FruitInsteadOfSeed)
            {
                // Replace seeds with fruit
                recipes["Elixir Of Revitalized Essence"] = elixirRecipe.Replace("114 1", "454 1");
            }
            else
            {
                // Ensure seeds are used
                recipes["Elixir Of Revitalized Essence"] = elixirRecipe.Replace("454 1", "114 1");
            }
        }

        Log("Crafting recipes edited based on runtime config", LogLevel.Trace);
    }

    /// <inheritdoc />
    public override bool ShouldApply()
    {
        return HasReachedStep(ProgressionStep.PartV);
    }
}
