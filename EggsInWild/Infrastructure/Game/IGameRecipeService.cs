namespace EggsInWild.Infrastructure.Game;

public interface IGameRecipeService
{
    bool HasCraftingRecipe(string recipeName);
    bool HasCookingRecipe(string recipeName);

    bool EnsureCraftingRecipeLearned(string recipeName);
    bool EnsureCookingRecipeLearned(string recipeName);

    bool ForgetCraftingRecipe(string recipeName);
    bool ForgetCookingRecipe(string recipeName);
}
