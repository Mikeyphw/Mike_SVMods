using StardewValley;

namespace WildEggsHatching.Infrastructure.Game;

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
}
