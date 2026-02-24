namespace EggsInWild.Infrastructure.Game;

public interface IGameRecipeService
{
    bool EnsureCraftingRecipeLearned(string recipeName);
}
