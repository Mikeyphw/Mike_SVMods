using System.Collections.Generic;
using System.Linq;
using StardewValley;

namespace WildEggsHatching.Infrastructure.Game;

public sealed class WorldLocations : IWorldLocations
{
    public IReadOnlyList<GameLocation> GetAll() => Game1.locations;

    public GameLocation? TryGetByName(string name)
        => Game1.locations.FirstOrDefault(l => l?.NameOrUniqueName == name || l?.Name == name);

    public bool Exists(string name) => TryGetByName(name) is not null;
}
