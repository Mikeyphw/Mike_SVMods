using StardewValley;

namespace EggsInWild.Infrastructure.Game;

public sealed class GameItemRegistry : IItemRegistry
{
    public bool Exists(string qualifiedItemId)
    {
        try
        {
            _ = ItemRegistry.Create(qualifiedItemId, 1);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
