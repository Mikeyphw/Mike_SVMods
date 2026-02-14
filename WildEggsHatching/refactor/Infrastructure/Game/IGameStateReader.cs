using StardewValley;

namespace WildEggsHatching.Infrastructure.Game;

/// <summary>
/// Provides read-only access to game state.
/// All game state reads should go through this interface.
/// This enables testing and isolation of domain logic.
/// </summary>
public interface IGameStateReader
{
    /// <summary>Gets the current game day (total days played).</summary>
    int GetCurrentDay();

    /// <summary>Gets the current daily luck value (-1.0 to 1.0).</summary>
    double GetDailyLuck();

    /// <summary>Gets the player's current location.</summary>
    GameLocation? GetPlayerLocation();

    /// <summary>Gets the player's tile position.</summary>
    (int X, int Y)? GetPlayerTile();

    /// <summary>Gets a game location by name.</summary>
    GameLocation? GetLocation(string name);

    /// <summary>Gets all game locations.</summary>
    System.Collections.Generic.IEnumerable<GameLocation> GetAllLocations();

    /// <summary>Gets the mine level (0 if not in mines).</summary>
    int GetMineLevel();

    /// <summary>Gets the skull cave level (0 if not in skull cave).</summary>
    int GetSkullCaveLevel();

    /// <summary>Checks if the day has started.</summary>
    bool HasStartedDay();

    /// <summary>Gets the current season (0=Spring, 1=Summer, 2=Fall, 3=Winter).</summary>
    int GetCurrentSeason();

    /// <summary>Gets the current year.</summary>
    int GetCurrentYear();

    /// <summary>Checks if the player has a specific quest.</summary>
    /// <param name="questId">The quest ID to check (string for custom quests, int for vanilla).</param>
    /// <returns>True if player has the quest.</returns>
    bool HasQuest(string questId);

    /// <summary>Checks if a specific quest is completed.</summary>
    /// <param name="questId">The quest ID to check (string for custom quests, int for vanilla).</param>
    /// <returns>True if the quest is completed.</returns>
    bool IsQuestCompleted(string questId);

    /// <summary>Checks if the player has received specific mail.</summary>
    bool HasMail(string mailId);

    /// <summary>Gets the player's current gold.</summary>
    int GetPlayerGold();

    /// <summary>Gets the player's total earned gold.</summary>
    long GetTotalEarnedGold();
}

