namespace WildEggsHatching.Infrastructure.Game;

public interface IItemRegistry
{
    bool Exists(string qualifiedItemId);
}
