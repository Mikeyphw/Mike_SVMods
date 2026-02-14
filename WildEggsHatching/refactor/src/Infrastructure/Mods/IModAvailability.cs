namespace WildEggsHatching.Infrastructure.Mods;

public interface IModAvailability
{
    bool IsLoaded(string uniqueId);
}
