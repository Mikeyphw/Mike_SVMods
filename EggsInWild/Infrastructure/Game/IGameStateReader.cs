using Microsoft.Xna.Framework;
using StardewValley;

namespace EggsInWild.Infrastructure.Game;

public interface IGameStateReader
{
    GameLocation GetLocation();
    Rectangle GetViewport();
    Farmer GetPlayer();

    /// <summary>Stable unique ID for the current save.</summary>
    long GetSaveUniqueId();

    /// <summary>Day number used for deterministic seeding (monotonic within a save).</summary>
    int GetDayNumber();

    int GetYear();
    string GetSeason();
    int GetDayOfMonth();

    int GetTimeOfDay();
}
