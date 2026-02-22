using System.Collections.Generic;
using StardewModdingAPI.Events;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Ids;
using Mikew.BirdsOfAFeather.Helpers.Content;
using Mikew.BirdsOfAFeather;

namespace WildEggsHatching.Infrastructure.Content.Editors;

public sealed class CraftingRecipesEditor : AssetEditorBase<ContentContext>
{
    public override string AssetName => Strings.AssetNames.CraftingRecipes;

    private const string ElixirRecipeKey = "Elixir Of Revitalized Essence";
    private const string IncubatorRecipeKey = "Wild Egg Incubator";

    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, string>().Data;

        // Original behavior: recipes are always defined; progression mode controls whether unlock conditions are handled externally.
        var unlock = context.Config.ProgressionMode ? "none" : "default";

        data[ElixirRecipeKey] = CreateElixirRecipe(context, unlock);
        data[IncubatorRecipeKey] = CreateIncubatorRecipe(context, unlock);
    }

    private static string CreateElixirRecipe(ContentContext context, string unlock)
        => CreateLegacyRecipeString(
            new List<(string itemId, int qty)>
            {
                ("118", 1),
                ((context.Config.FruitInsteadOfSeed ? "454" : "114"), 1),
                ("734", 1),
                ("107", 1),
            },
            (ModItemIds.ElixirOfRevitalizedEssence, 1),
            isBigCraftable: false,
            unlockConditions: unlock
        );

    private static string CreateIncubatorRecipe(ContentContext context, string unlock)
    {
        var ingredients = new List<(string itemId, int qty)>
        {
            ("390", 80),
            ("118", 20),
            (ModItemIds.ElixirOfRevitalizedEssence, 1),
        };

        //  if the incubator doesn't consume ferns per-use, add a fern cost to craft it.
        if (!context.Config.NeedsFern)
            ingredients.Add(("259", 3));

        return CreateLegacyRecipeString(
            ingredients,
            (ModItemIds.WildEggIncubator, 1),
            isBigCraftable: true,
            unlockConditions: unlock
        );
    }

    private static string CreateLegacyRecipeString(
        IReadOnlyList<(string itemId, int qty)> ingredients,
        (string outputId, int qty) output,
        bool isBigCraftable,
        string unlockConditions)
    {
        var parts = new List<string>(ingredients.Count);
        foreach (var (itemId, qty) in ingredients)
            parts.Add($"{itemId} {qty}");

        var ingredientsString = string.Join(' ', parts);
        var outputString = $"{output.outputId} {output.qty}";

        // Recipe format: "<ingredients>//<output>/<isBigCraftable>/<unlockConditions>/"
        return $"{ingredientsString}//{outputString}/{(isBigCraftable ? "true" : "false")}/{unlockConditions}/";
    }
}
