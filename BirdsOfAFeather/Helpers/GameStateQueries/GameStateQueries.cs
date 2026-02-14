namespace Mikew.BirdsOfAFeather.Helpers.GameStateQueries;

/// <summary>
/// Mail types
/// </summary>
public enum MailType
{
    /// <summary>
    /// Any mail in the mailbox, in the queue for tomorrow's mailbox, or already received.
    /// </summary>
    Any,

    /// <summary>
    /// Mail in the mailbox.
    /// </summary>
    Mailbox,

    /// <summary>
    /// Mail in the queue for tomorrow's mailbox.
    /// </summary>
    Tomorrow,

    /// <summary>
    /// Mail which either was in the mailbox and read by the player; or has no letter in Data/mail, so it was added to the received list directly.
    /// </summary>
    Received
}

/// <summary>
/// NPC relationship types
/// </summary>
public static class NPCLationshipType
{
    /// <summary>
    /// Friendship level with an NPC.
    /// </summary>
    public const string Friendship = "Friendship";

    /// <summary>
    /// Romantic relationship level with an NPC.
    /// </summary>
    public const string Romance = "Romance";

    /// <summary>
    /// Marriage status with an NPC.
    /// </summary>
    public const string Marriage = "Marriage";
}

/// <summary>
/// World state queries
/// </summary>
public static class WorldStateQuery
{
    /// <summary>
    /// Whether the player has cracked open any Golden Coconuts (true or false).
    /// </summary>
    public const string GoldenCoconutCracked = "GoldenCoconutCracked";

    /// <summary>
    /// The total number of Golden Walnuts found or held by any player. To check how many one player currently holds, see the PLAYER_HAS_ITEM query.
    /// </summary>
    public const string GoldenWalnutsFound = "GoldenWalnutsFound";

    /// <summary>
    /// Whether the Henchman has been removed, so the player can access Witch's Hut (true or false).
    /// </summary>
    public const string IsGoblinRemoved = "IsGoblinRemoved";

    /// <summary>
    /// Whether the Night Market submarine is currently in use by a player (true or false).
    /// </summary>
    public const string IsSubmarineLocked = "IsSubmarineLocked";

    /// <summary>
    /// The total number of Lost Books found or held by any player.
    /// </summary>
    public const string LostBooksFound = "LostBooksFound";

    /// <summary>
    /// The current Shrine of Challenge difficulty level for the mine.
    /// </summary>
    public const string MinesDifficulty = "MinesDifficulty";

    /// <summary>
    /// The current Shrine of Challenge difficulty level for the Skull Cavern.
    /// </summary>
    public const string SkullCavesDifficulty = "SkullCavesDifficulty";

    /// <summary>
    /// The number of times the player has obtained a Mini-Shipping Bin.
    /// </summary>
    public const string MiniShippingBinsObtained = "MiniShippingBinsObtained";

    /// <summary>
    /// Whether the player has unlocked Ginger Island parrot platforms, regardless of whether they've completed them (true or false).
    /// </summary>
    public const string ParrotPlatformsUnlocked = "ParrotPlatformsUnlocked";

    /// <summary>
    /// The multiplayer connection privacy mode (InviteOnly or FriendsOnly).
    /// </summary>
    public const string ServerPrivacy = "ServerPrivacy";

    /// <summary>
    /// The value of the 'mine rewards' game option (Default or Remixed).
    /// </summary>
    public const string ShuffleMineChests = "ShuffleMineChests";

    /// <summary>
    /// The weather ID for tomorrow in the main valley area.
    /// </summary>
    public const string WeatherForTomorrow = "WeatherForTomorrow";

    /// <summary>
    /// The number of times the Traveling Cart will visit before Red Cabbage is guaranteed to drop.
    /// </summary>
    public const string VisitsUntilY1Guarantee = "VisitsUntilY1Guarantee";
}

/// <summary>
/// Location types
/// </summary>
public static class LocationType
{
    /// <summary>
    /// The location containing the current player (regardless of the target player).
    /// </summary>
    public const string Here = "Here";

    /// <summary>
    /// The location containing the in-game entity being edited (e.g. the machine for Data/Machines or fruit tree for Data/FruitTrees).
    /// </summary>
    public const string Target = "Target";
}

/// <summary>
/// Player types
/// </summary>
public static class PlayerType
{
    /// <summary>
    /// At least one player must match the condition, regardless of whether they're online.
    /// </summary>
    public const string Any = "Any";

    /// <summary>
    /// Every player must match the condition, regardless of whether they're online.
    /// </summary>
    public const string All = "All";

    /// <summary>
    /// The local player.
    /// </summary>
    public const string Current = "Current";

    /// <summary>
    /// The main player.
    /// </summary>
    public const string Host = "Host";
}

public class GameStateQueryHelper
{
    private List<string> conditions = new List<string>();

    public GameStateQueryHelper Any(params GameStateQueryHelper[] queries)
    {
        foreach (var query in queries)
        {
            conditions.Add("(" + query.ToString() + ")");
        }

        conditions.Add("OR");

        return this;
    }

    public GameStateQueryHelper And(params string[] conditions)
    {
        this.conditions.AddRange(conditions);

        return this;
    }

    public GameStateQueryHelper Not(string condition)
    {
        conditions.Add("!" + condition);

        return this;
    }

    public GameStateQueryHelper Add(string condition)
    {
        conditions.Add(condition);

        return this;
    }

    public override string ToString()
    {
        return string.Join(" ", conditions);
    }
}
