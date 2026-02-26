namespace EggsInWild.Infrastructure.Game;

public interface IGameRecipeService
{
    bool EnsureCraftingRecipeLearned(string recipeName);
    bool EnsureCookingRecipeLearned(string recipeName);

    bool ForgetCraftingRecipe(string recipeName);
    bool ForgetCookingRecipe(string recipeName);
}
