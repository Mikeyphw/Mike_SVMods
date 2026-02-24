using System.Collections.Generic;
using StardewValley;

namespace EggsInWild.Infrastructure.Game;

public interface IWorldLocations
{
    IReadOnlyList<GameLocation> GetAll();
    GameLocation? TryGetByName(string name);
    bool Exists(string name);
}
