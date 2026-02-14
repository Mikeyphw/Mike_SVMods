namespace Mikew.BirdsOfAFeather.Helpers.GameStateQueries;

/// <summary>
/// A query condition related to the world.
/// </summary>
public static class WorldCondition
{
    /// <summary>
    /// Whether the number of matching buildings is within a range.
    /// </summary>
    /// <param name="locations">A target location, or "All" to count buildings in all locations.</param>
    /// <param name="buildingType">The building ID in Data/Buildings to count, or "All" to count all building types.</param>
    /// <param name="min">The minimum count to require (default 1).</param>
    /// <param name="max">The maximum count to require (default unlimited).</param>
    /// <param name="countUnbuilt">Whether to count buildings that haven't been fully constructed yet.</param>
    /// <returns>The current query instance.</returns>
    public static string BuildingsConstructed(
        string locations,
        string buildingType,
        int min,
        int max = int.MaxValue,
        bool countUnbuilt = false
    )
    {
        return $"BUILDINGS_CONSTRUCTED {locations} {buildingType} {min} {max} {(countUnbuilt ? "true" : "false")}";
    }

    /// <summary>
    /// Whether players can build more cabins (i.e. they haven't reached the maximum number of player slots yet).
    /// </summary>
    /// <returns>The current query instance.</returns>
    public const string CanBuildCabin = "CAN_BUILD_CABIN";

    /// <summary>
    /// Whether there are fewer of the given building constructed than there are cabins.
    /// </summary>
    /// <param name="buildingId">The building ID to check.</param>
    /// <returns>The current query instance.</returns>
    public static string CanBuildForCabins(string buildingId)
    {
        return $"CAN_BUILD_FOR_CABINS {buildingId}";
    }

    /// <summary>
    /// The current farm cave.
    /// </summary>
    /// <param name="types">One or more farm cave types (Bats, Mushrooms, or None).</param>
    /// <returns>The current query instance.</returns>
    public static string FarmCave(params string[] types)
    {
        return $"FARM_CAVE {string.Join(" ", types)}";
    }

    /// <summary>
    /// The name of the farm.
    /// </summary>
    /// <param name="name">The farm name to check.</param>
    /// <returns>The current query instance.</returns>
    public static string FarmName(string name)
    {
        return $"FARM_NAME {name}";
    }

    /// <summary>
    /// The farm type.
    /// </summary>
    /// <param name="types">One or more farm types (numeric ID, readable key, or custom farm type ID).</param>
    /// <returns>The current query instance.</returns>
    public static string FarmType(params string[] types)
    {
        return $"FARM_TYPE {string.Join(" ", types)}";
    }

    /// <summary>
    /// Whether all the Lost Books for the museum have been found.
    /// </summary>
    /// <returns>The current query instance.</returns>
    public const string FoundAllLostBooks = "FOUND_ALL_LOST_BOOKS";

    /// <summary>
    /// Whether the 'Target' location is explicitly set for the current context (ignoring the fallback to the current player's location).
    /// </summary>
    /// <returns>The current query instance.</returns>
    public const string HasTargetLocation = "HAS_TARGET_LOCATION";

    /// <summary>
    /// Whether the community center has been repaired.
    /// </summary>
    /// <returns>The current query instance.</returns>
    public const string IsCommunityCenterComplete = "IS_COMMUNITY_CENTER_COMPLETE";

    /// <summary>
    /// Whether the farm type is a custom one created by a mod.
    /// </summary>
    /// <returns>The current query instance.</returns>
    public const string IsCustomFarmType = "IS_CUSTOM_FARM_TYPE";

    /// <summary>
    /// Whether the current player is the main/host player.
    /// </summary>
    /// <returns>The current query instance.</returns>
    public const string IsHost = "IS_HOST";

    /// <summary>
    /// Whether the North Ginger Island bridge to the dig site has been repaired.
    /// </summary>
    /// <returns>The current query instance.</returns>
    public const string IsIslandNorthBridgeFixed = "IS_ISLAND_NORTH_BRIDGE_FIXED";

    /// <summary>
    /// Whether the Joja warehouse has been built.
    /// </summary>
    /// <returns>The current query instance.</returns>
    public const string IsJojaMartComplete = "IS_JOJA_MART_COMPLETE";

    /// <summary>
    /// Whether the game is currently in multiplayer mode (regardless of whether there's multiple players connected).
    /// </summary>
    /// <returns>The current query instance.</returns>
    public const string IsMultiplayer = "IS_MULTIPLAYER";

    /// <summary>
    /// Whether the named NPC is visiting Ginger Island today.
    /// </summary>
    /// <param name="name">The NPC name to check.</param>
    /// <returns>The current query instance.</returns>
    public static string IsVisitingIsland(string name)
    {
        return $"IS_VISITING_ISLAND {name}";
    }

    /// <summary>
    /// Whether the given location is accessible.
    /// </summary>
    /// <param name="name">The location name to check.</param>
    /// <returns>The current query instance.</returns>
    public static string LocationAccessible(string name)
    {
        return $"LOCATION_ACCESSIBLE {name}";
    }

    /// <summary>
    /// The location context ID for the given location.
    /// </summary>
    /// <param name="location">The location to check.</param>
    /// <param name="contextId">The context ID to check.</param>
    /// <returns>The current query instance.</returns>
    public static string LocationContext(string location, string contextId)
    {
        return $"LOCATION_CONTEXT {location} {contextId}";
    }

    /// <summary>
    /// Checks to see if the location has a given value in its CustomFields.
    /// </summary>
    /// <param name="location">The location to check.</param>
    /// <param name="key">The key to check.</param>
    /// <param name="value">The value to check (optional).</param>
    /// <returns>The current query instance.</returns>
    public static string LocationHasCustomField(string location, string key, string? value = null)
    {
        return $"LOCATION_HAS_CUSTOM_FIELD {location} {key} {(value != null ? value : string.Empty)}";
    }

    /// <summary>
    /// Whether the given location is indoors, outdoors, in the mines, or in the Skull Cavern.
    /// </summary>
    /// <param name="location">The location to check.</param>
    /// <param name="type">The location type to check (indoors, outdoors, mines, or Skull Cavern).</param>
    /// <returns>The current query instance.</returns>
    public static string LocationIs(string location, string type)
    {
        return $"LOCATION_IS_{type.ToUpper()} {location}";
    }

    /// <summary>
    /// Whether the given location has the specified name or unique instanced name.
    /// </summary>
    /// <param name="location">The location to check.</param>
    /// <param name="name">The name to check.</param>
    /// <returns>The current query instance.</returns>
    public static string LocationName(string location, string name)
    {
        return $"LOCATION_NAME {location} {name}";
    }

    /// <summary>
    /// Whether the given location is in one of the given seasons.
    /// </summary>
    /// <param name="location">The location to check.</param>
    /// <param name="seasons">One or more seasons to check (spring, summer, fall, or winter).</param>
    /// <returns>The current query instance.</returns>
    public static string LocationSeason(string location, params string[] seasons)
    {
        return $"LOCATION_SEASON {location} {string.Join(" ", seasons)}";
    }

    /// <summary>
    /// Whether all players have donated a total of <min count> to [max count] (default unlimited) inclusively to the museum.
    /// </summary>
    /// <param name="minCount">The minimum count to require.</param>
    /// <param name="maxCount">The maximum count to require (default unlimited).</param>
    /// <param name="objectTypes">One or more object types to filter by (optional).</param>
    /// <returns>The current query instance.</returns>
    public static string MuseumDonations(
        int minCount,
        int maxCount = int.MaxValue,
        params string[] objectTypes
    )
    {
        return $"MUSEUM_DONATIONS {minCount} {maxCount} {string.Join(" ", objectTypes)}";
    }

    /// <summary>
    /// The weather ID in the given location.
    /// </summary>
    /// <param name="location">The location to check.</param>
    /// <param name="weather">The weather to check.</param>
    /// <returns>The current query instance.</returns>
    public static string Weather(string location, string weather)
    {
        return $"WEATHER {location} {weather}";
    }

    /// <summary>
    /// Whether a property on Game1.netWorldState has the given value.
    /// </summary>
    /// <param name="name">The property name to check.</param>
    /// <param name="value">The value to check.</param>
    /// <returns>The current query instance.</returns>
    public static string WorldStateField(string name, string value)
    {
        return $"WORLD_STATE_FIELD {name} {value}";
    }

    /// <summary>
    /// Whether a numeric property on Game1.netWorldState has a value between <min> and [max] (default unlimited).
    /// </summary>
    /// <param name="name">The property name to check.</param>
    /// <param name="min">The minimum value to check.</param>
    /// <param name="max">The maximum value to check (default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string WorldStateField(string name, int min, int max = int.MaxValue)
    {
        return $"WORLD_STATE_FIELD {name} {min} {max}";
    }

    /// <summary>
    /// Whether any world state flag matching the given <id> values is set.
    /// </summary>
    /// <param name="ids">One or more world state flag IDs to check.</param>
    /// <returns>The current query instance.</returns>
    public static string WorldStateId(params string[] ids)
    {
        return $"WORLD_STATE_ID {string.Join(" ", ids)}";
    }
}
