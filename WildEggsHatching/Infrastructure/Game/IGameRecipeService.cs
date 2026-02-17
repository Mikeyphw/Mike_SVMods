namespace WildEggsHatching.Infrastructure.Game;

public interface IGameRecipeService
{
    bool EnsureCraftingRecipeLearned(string recipeName);
}
