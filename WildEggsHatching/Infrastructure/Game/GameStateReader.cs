using Microsoft.Xna.Framework;
using StardewValley;

namespace WildEggsHatching.Infrastructure.Game;

public sealed class GameStateReader : IGameStateReader
{
    public GameLocation GetLocation() => Game1.currentLocation;
    public Rectangle GetViewport() => Game1.viewport;
    public Farmer GetPlayer() => Game1.player;

    public long GetSaveUniqueId() => Game1.uniqueIDForThisGame;

    public int GetDayNumber()
    {
        // Stable-ish within a save: year * 112 + seasonIndex * 28 + dayOfMonth
        // (matches SDV's 28-day seasons).
        var seasonIndex = Game1.seasonIndex;
        return (Game1.year * 112) + (seasonIndex * 28) + Game1.dayOfMonth;
    }

    public int GetYear() => Game1.year;
    public string GetSeason() => Game1.currentSeason;
    public int GetDayOfMonth() => Game1.dayOfMonth;

    public int GetTimeOfDay() => Game1.timeOfDay;
}
