using StardewModdingAPI;

namespace WildEggsHatching.Infrastructure.Mods;

public sealed class SmapiModAvailability : IModAvailability
{
    private readonly IModRegistry _mods;

    public SmapiModAvailability(IModRegistry mods)
    {
        _mods = mods;
    }

    public bool IsLoaded(string uniqueId)
        => uniqueId == "Vanilla" || _mods.IsLoaded(uniqueId);
}
