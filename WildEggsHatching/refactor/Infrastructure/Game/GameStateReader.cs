using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Locations;

namespace WildEggsHatching.Infrastructure.Game;

/// <summary>
/// Implementation of IGameStateReader that reads from Game1 and related APIs.
/// This is the only place in the infrastructure layer that directly accesses Game1.
/// </summary>
public sealed class GameStateReader : IGameStateReader
{
    /// <inheritdoc />
    public int GetCurrentDay()
    {
        return Game1.Date.TotalDays;
    }

    /// <inheritdoc />
    public double GetDailyLuck()
    {
        return Game1.dailyLuck;
    }

    /// <inheritdoc />
    public GameLocation? GetPlayerLocation()
    {
        return Game1.player?.currentLocation;
    }

    /// <inheritdoc />
    public (int X, int Y)? GetPlayerTile()
    {
        var player = Game1.player;
        if (player == null)
            return null;

        return ((int)player.Tile.X, (int)player.Tile.Y);
    }

    /// <inheritdoc />
    public GameLocation? GetLocation(string name)
    {
        return Game1.getLocationFromName(name);
    }

    /// <inheritdoc />
    public IEnumerable<GameLocation> GetAllLocations()
    {
        return Game1.locations;
    }

    /// <inheritdoc />
    public int GetMineLevel()
    {
        var mineShaft = GetPlayerLocation() as MineShaft;
        return mineShaft?.mineLevel ?? 0;
    }

    /// <inheritdoc />
    public int GetSkullCaveLevel()
    {
        var mineShaft = GetPlayerLocation() as MineShaft;
        return mineShaft?.GetSkullCaveLevel() ?? 0;
    }

    /// <inheritdoc />
    public bool HasStartedDay()
    {
        return Game1.hasStartedDay;
    }

    /// <inheritdoc />
    public int GetCurrentSeason()
    {
        return (int)Game1.currentSeason;
    }

    /// <inheritdoc />
    public int GetCurrentYear()
    {
        return Game1.year;
    }

    /// <inheritdoc />
    public bool HasQuest(string questId)
    {
        if (Game1.player == null)
            return false;

        // Try parsing as int for vanilla quests
        if (int.TryParse(questId, out int intQuestId))
        {
            return Game1.player.hasQuest(intQuestId);
        }

        // For string quest IDs, check custom quest log
        return Game1.player.questLog.Any(q => q.id.Value == questId);
    }

    /// <inheritdoc />
    public bool IsQuestCompleted(string questId)
    {
        if (Game1.player == null)
            return false;

        // Try parsing as int for vanilla quests
        if (int.TryParse(questId, out int intQuestId))
        {
            return Game1.player.questLog.Any(q => q.id.Value == intQuestId && q.completed.Value);
        }

        // For string quest IDs, check custom quest log
        return Game1.player.questLog.Any(q => q.id.Value == questId && q.completed.Value);
    }

    /// <inheritdoc />
    public bool HasMail(string mailId)
    {
        return Game1.player?.mailReceived.Contains(mailId) ?? false;
    }

    /// <inheritdoc />
    public int GetPlayerGold()
    {
        return Game1.player?.Money ?? 0;
    }

    /// <inheritdoc />
    public long GetTotalEarnedGold()
    {
        return Game1.player?.TotalMoneyEarned ?? 0;
    }
}

