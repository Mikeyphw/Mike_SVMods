using StardewValley;

namespace EggsInWild.Infrastructure.Game;

public sealed class GameRecipeService : IGameRecipeService
{
    public bool EnsureCraftingRecipeLearned(string recipeName)
    {
        if (string.IsNullOrWhiteSpace(recipeName))
            return false;

        var player = Game1.player;
        if (player is null)
            return false;

        if (player.craftingRecipes.ContainsKey(recipeName))
            return false;

        player.craftingRecipes[recipeName] = 0;
        return true;
    }

    public bool EnsureCookingRecipeLearned(string recipeName)
    {
        if (string.IsNullOrWhiteSpace(recipeName))
            return false;

        var player = Game1.player;
        if (player is null)
            return false;

        if (player.cookingRecipes.ContainsKey(recipeName))
            return false;

        player.cookingRecipes[recipeName] = 0;
        return true;
    }

    public bool ForgetCraftingRecipe(string recipeName)
    {
        if (string.IsNullOrWhiteSpace(recipeName))
            return false;

        var player = Game1.player;
        if (player is null)
            return false;

        return player.craftingRecipes.Remove(recipeName);
    }

    public bool ForgetCookingRecipe(string recipeName)
    {
        if (string.IsNullOrWhiteSpace(recipeName))
            return false;

        var player = Game1.player;
        if (player is null)
            return false;

        return player.cookingRecipes.Remove(recipeName);
    }
}
