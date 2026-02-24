namespace EggsInWild.Infrastructure.Mods;

public interface IModAvailability
{
    bool IsLoaded(string uniqueId);
}
