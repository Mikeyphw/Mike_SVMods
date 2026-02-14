namespace Mikew.BirdsOfAFeather.Helpers.GameStateQueries;

public static class PlayerRelationships
{
    /// <summary>
    /// Whether the specified player(s) have a friend with friendship points between <min points> and [max points] (default unlimited) inclusively.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="npc">The NPC's internal name, Any (check every NPC), or AnyDateable (check every romanceable NPC).</param>
    /// <param name="minPoints">The minimum points to require.</param>
    /// <param name="maxPoints">The maximum points to require (default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string PlayerFriendshipPoints(string player, string npc, int minPoints, int maxPoints = int.MaxValue)
    {
        return $"PLAYER_FRIENDSHIP_POINTS {player} {npc} {minPoints} {maxPoints}";
        
    }

    /// <summary>
    /// Whether the specified player(s) have a number of children between [min] (default 1) and [max] (default unlimited) inclusively.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="minChildren">The minimum number of children to require (default 1).</param>
    /// <param name="maxChildren">The maximum number of children to require (default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string PlayerHasChildren(string player, int minChildren = 1, int maxChildren = int.MaxValue)
    {
        return $"PLAYER_HAS_CHILDREN {player} {minChildren} {maxChildren}";
        
    }

    /// <summary>
    /// Whether the specified player(s) have a pet.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <returns>The current query instance.</returns>
    public static string PlayerHasPet(string player)
    {
        return $"PLAYER_HAS_PET {player}";
        
    }

    /// <summary>
    /// Whether the specified player(s) have a friend with a heart level between <min hearts> and [max hearts] (default unlimited) inclusively.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="npc">The NPC's internal name, Any (check every NPC), or AnyDateable (check every romanceable NPC).</param>
    /// <param name="minHearts">The minimum heart level to require.</param>
    /// <param name="maxHearts">The maximum heart level to require (default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string PlayerHearts(string player, string npc, int minHearts, int maxHearts = int.MaxValue)
    {
        return $"PLAYER_HEARTS {player} {npc} {minHearts} {maxHearts}";
        
    }

    /// <summary>
    /// Whether the specified player(s) have talked to one of the given NPCs at least once.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="npcs">The NPC's internal names.</param>
    /// <returns>The current query instance.</returns>
    public static string PlayerHasMet(string player, params string[] npcs)
    {
        return$"PLAYER_HAS_MET {player} {string.Join(" ", npcs)}";
        
    }

    /// <summary>
    /// Whether the specified player(s) have one of the relationship statuses with an NPC.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="npc">The NPC's internal name or Any (match any NPC).</param>
    /// <param name="types">The relationship types.</param>
    /// <returns>The current query instance.</returns>
    public static string PlayerNpcRelationship(string player, string npc, params string[] types)
    {
        return$"PLAYER_NPC_RELATIONSHIP {player} {npc} {string.Join(" ", types)}";
        
    }

    /// <summary>
    /// Whether the specified player(s) have one of the relationship types with the other specified player(s).
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="otherPlayer">The other player or Any (match any player).</param>
    /// <param name="types">The relationship types.</param>
    /// <returns>The current query instance.</returns>
    public static string PlayerPlayerRelationship(string player, string otherPlayer, params string[] types)
    {
        return$"PLAYER_PLAYER_RELATIONSHIP {player} {otherPlayer} {string.Join(" ", types)}";
        
    }

    /// <summary>
    /// Whether the preferred pet for the specified player(s) is one of the given types.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="petTypes">The pet types.</param>
    /// <returns>The current query instance.</returns>
    public static string PlayerPreferredPet(string player, params string[] petTypes)
    {
        return$"PLAYER_PREFERRED_PET {player} {string.Join(" ", petTypes)}";
        
    }
}