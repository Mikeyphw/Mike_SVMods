namespace Mikew.NestCore.Helpers.GameStateQueries;

public static class PlayerCondition
{
    public const string Current = "Current";

    /// <summary>
    /// Whether any player has reached a level between <min> and [max] (default unlimited) inclusively in the mines.
    /// </summary>
    /// <param name="min">The minimum level to require.</param>
    /// <param name="max">The maximum level to require (default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string MineLowestLevelReached(int min, int max = int.MaxValue)
    {
        return $"MINE_LOWEST_LEVEL_REACHED {min} {max}";
    }

    /// <summary>
    /// Whether the specified player(s) have a combat level between <min> and [max] (default unlimited) inclusively.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="min">The minimum level to require.</param>
    /// <param name="max">The maximum level to require (default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string CombatLevel(int min, int max = int.MaxValue, string player = Current)
    {
        return $"PLAYER_COMBAT_LEVEL {player} {min} {max}";
    }

    /// <summary>
    /// Whether the specified player(s) have a farming level between <min> and [max] (default unlimited) inclusively.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="min">The minimum level to require.</param>
    /// <param name="max">The maximum level to require (default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string FarmingLevel(
        int min,
        int max = int.MaxValue,
        string player = Current
    )
    {
        return $"PLAYER_FARMING_LEVEL {player} {min} {max}";
    }

    /// <summary>
    /// Whether the specified player(s) have a fishing level between <min> and [max] (default unlimited) inclusively.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="min">The minimum level to require.</param>
    /// <param name="max">The maximum level to require (default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string FishingLevel(
        int min,
        int max = int.MaxValue,
        string player = Current
    )
    {
        return $"PLAYER_FISHING_LEVEL {player} {min} {max}";
    }

    /// <summary>
    /// Whether the specified player(s) have a foraging level between <min> and [max] (default unlimited) inclusively.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="min">The minimum level to require.</param>
    /// <param name="max">The maximum level to require (default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string ForagingLevel(
        int min,
        int max = int.MaxValue,
        string player = Current
    )
    {
        return $"PLAYER_FORAGING_LEVEL {player} {min} {max}";
    }

    /// <summary>
    /// Whether the specified player(s) have a luck level between <min> and [max] (default unlimited) inclusively.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="min">The minimum level to require.</param>
    /// <param name="max">The maximum level to require (default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string LuckLevel(int min, int max = int.MaxValue, string player = Current)
    {
        return $"PLAYER_LUCK_LEVEL {player} {min} {max}";
    }

    /// <summary>
    /// Whether the specified player(s) have a mining level between <min> and [max] (default unlimited) inclusively.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="min">The minimum level to require.</param>
    /// <param name="max">The maximum level to require (default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string MiningLevel(int min, int max = int.MaxValue, string player = Current)
    {
        return $"PLAYER_MINING_LEVEL {player} {min} {max}";
    }

    /// <summary>
    /// Whether the specified player(s) have between <min> and [max] (default unlimited) gold inclusively.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="min">The minimum amount of gold to require.</param>
    /// <param name="max">The maximum amount of gold to require (default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string CurrentMoney(
        int min,
        int max = int.MaxValue,
        string player = Current
    )
    {
        return $"PLAYER_CURRENT_MONEY {player} {min} {max}";
    }

    /// <summary>
    /// Whether the specified player(s) have upgraded their farmhouse or cabin to a level between <min> and [max] (default unlimited) inclusively.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="min">The minimum upgrade level to require.</param>
    /// <param name="max">The maximum upgrade level to require (default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string FarmhouseUpgrade(
        int min,
        int max = int.MaxValue,
        string player = Current
    )
    {
        return $"PLAYER_FARMHOUSE_UPGRADE {player} {min} {max}";
    }

    /// <summary>
    /// Whether the specified player(s) are Male or Female.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="gender">The gender to require (Male or Female).</param>
    /// <returns>The current query instance.</returns>
    public static string Gender(string gender, string player = Current)
    {
        return $"PLAYER_GENDER {player} {gender}";
    }

    /// <summary>
    /// Whether the specified player(s) have unlocked a specific achievement ID.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="achievementId">The achievement ID to require.</param>
    /// <returns>The current query instance.</returns>
    public static string HasAchievement(string achievementId, string player = Current)
    {
        return $"PLAYER_HAS_ACHIEVEMENT {player} {achievementId}";
    }

    /// <summary>
    /// Whether the specified player(s) have unlocked every achievement listed in Data/Achievements.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <returns>The current query instance.</returns>
    public static string HasAllAchievements(string player = Current)
    {
        return $"PLAYER_HAS_ALL_ACHIEVEMENTS {player}";
    }

    /// <summary>
    /// Whether the specified player(s) have a given buff ID currently applied.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="buffId">The buff ID to require.</param>
    /// <returns>The current query instance.</returns>
    public static string HasBuff(string buffId, string player = Current)
    {
        return $"PLAYER_HAS_BUFF {player} {buffId}";
    }

    /// <summary>
    /// Whether the specified player(s) have caught at least one fish with the given ID.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="fishId">The fish ID to require.</param>
    /// <returns>The current query instance.</returns>
    public static string HasCaughtFish(string fishId, string player = Current)
    {
        return $"PLAYER_HAS_CAUGHT_FISH {player} {fishId}";
    }

    /// <summary>
    /// Whether the specified player(s) have a conversation topic with the ID <id> active.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="conversationId">The conversation ID to require.</param>
    /// <returns>The current query instance.</returns>
    public static string HasConversationTopic(string conversationId, string player = Current)
    {
        return $"PLAYER_HAS_CONVERSATION_TOPIC {player} {conversationId}";
    }

    /// <summary>
    /// Whether the specified player(s) know the cooking recipe identified by its internal name.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="recipeName">The recipe name to require.</param>
    /// <returns>The current query instance.</returns>
    public static string HasCookingRecipe(string recipeName, string player = Current)
    {
        return $"PLAYER_HAS_COOKING_RECIPE {player} {recipeName}";
    }

    /// <summary>
    /// Whether the specified player(s) know the crafting recipe identified by its internal name.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="recipeName">The recipe name to require.</param>
    /// <returns>The current query instance.</returns>
    public static string HasCraftingRecipe(string recipeName, string player = Current)
    {
        return $"PLAYER_HAS_CRAFTING_RECIPE {player} {recipeName}";
    }

    /// <summary>
    /// Whether the specified player(s) have chosen the given dialogue answer in a previous dialogue.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="dialogueId">The dialogue ID to require.</param>
    /// <returns>The current query instance.</returns>
    public static string HasDialogueAnswer(string dialogueId, string player = Current)
    {
        return $"PLAYER_HAS_DIALOGUE_ANSWER {player} {dialogueId}";
    }

    /// <summary>
    /// Whether the specified player(s) have heard a song track's cue name.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="songId">The song ID to require.</param>
    /// <returns>The current query instance.</returns>
    public static string HasHeardSong(string songId, string player = Current)
    {
        return $"PLAYER_HAS_HEARD_SONG {player} {songId}";
    }

    /// <summary>
    /// Whether the specified player(s) have between [min] and [max] (default unlimited) matching items in their inventory, inclusively.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="itemId">The item ID to require.</param>
    /// <param name="min">The minimum amount of items to require.</param>
    /// <param name="max">The maximum amount of items to require (default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string HasItem(
        string itemId,
        int min,
        int max = int.MaxValue,
        string player = Current
    )
    {
        return $"PLAYER_HAS_ITEM {player} {itemId} {min} {max}";
    }

    /// <summary>
    /// Whether the specified player(s) have the given mail flag set.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="mailId">The mail ID to require.</param>
    /// <param name="type">The type of mail to require (default Any).</param>
    /// <returns>The current query instance.</returns>
    public static string HasMail(
        string mailId,
        MailType type = MailType.Received,
        string player = "Host"
    )
    {
        return $"PLAYER_HAS_MAIL {player} {mailId} {Enum.GetName(type)}";
    }

    /// <summary>
    /// Whether the specified player(s) have the given profession ID.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="professionId">The profession ID to require.</param>
    /// <returns>The current query instance.</returns>
    public static string HasProfession(string professionId, string player = Current)
    {
        return $"PLAYER_HAS_PROFESSION {player} {professionId}";
    }

    /// <summary>
    /// Whether the specified player(s) have applied the trigger action with the given <id>.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="id">The trigger action ID to require.</param>
    /// <returns>The current query instance.</returns>
    public static string HasRunTriggerAction(string id, string player = Current)
    {
        return $"PLAYER_HAS_RUN_TRIGGER_ACTION {player} {id}";
    }

    /// <summary>
    /// Whether the specified player(s) have read a secret note, where <id> is the secret note's integer ID.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="id">The secret note ID to require.</param>
    /// <returns>The current query instance.</returns>
    public static string HasSecretNote(string id, string player = Current)
    {
        return $"PLAYER_HAS_SECRET_NOTE {player} {id}";
    }

    /// <summary>
    /// Whether the specified player(s) have seen the event with the given <id>.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="id">The event ID to require.</param>
    /// <returns>The current query instance.</returns>
    public static string HasSeenEvent(string id, string player = Current)
    {
        return $"PLAYER_HAS_SEEN_EVENT {player} {id}";
    }

    /// <summary>
    /// Whether the specified player(s) have the town key.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <returns>The current query instance.</returns>
    public static string HasTownKey(string player = Current)
    {
        return $"PLAYER_HAS_TOWN_KEY {player}";
    }

    /// <summary>
    /// Whether the specified player(s) have a trash can upgrade level between <min> and [max] (default unlimited) inclusively.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="min">The minimum trash can level to require.</param>
    /// <param name="max">The maximum trash can level to require (default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string HasTrashCanLevel(
        int min,
        int max = int.MaxValue,
        string player = Current
    )
    {
        return $"PLAYER_HAS_TRASH_CAN_LEVEL {player} {min} {max}";
    }

    /// <summary>
    /// Whether the specified player(s) are in the given location context.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="locationContext">The location context to require.</param>
    /// <returns>The current query instance.</returns>
    public static string LocationContext(string locationContext, string player = Current)
    {
        return $"PLAYER_LOCATION_CONTEXT {player} {locationContext}";
    }

    /// <summary>
    /// Whether the specified player(s) are in the given location, using the name or unique instanced name.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="locationName">The location name to require.</param>
    /// <returns>The current query instance.</returns>
    public static string LocationName(string locationName, string player = Current)
    {
        return $"PLAYER_LOCATION_NAME {player} {locationName}";
    }

    /// <summary>
    /// Whether the specified player(s) are in the given location, using the unique instanced name.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="locationName">The location name to require.</param>
    /// <returns>The current query instance.</returns>
    public static string LocationUniqueName(string locationName, string player = Current)
    {
        return $"PLAYER_LOCATION_UNIQUE_NAME {player} {locationName}";
    }

    /// <summary>
    /// Whether the specified player(s) have a player.modData entry added by a mod with thegiven <key> and <value>.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="key">The mod data key to require.</param>
    /// <param name="value">The mod data value to require.</param>
    /// <returns>The current query instance.</returns>
    public static string ModData(string key, string value, string player = Current)
    {
        return $"PLAYER_MOD_DATA {player} {key} {value}";
    }

    /// <summary>
    /// Whether the specified player(s) have earned between <min> and [max] (default unlimited) gold inclusively.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="min">The minimum amount of gold to require.</param>
    /// <param name="max">The maximum amount of gold to require (default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string MoneyEarned(int min, int max = int.MaxValue, string player = Current)
    {
        return $"PLAYER_MONEY_EARNED {player} {min} {max}";
    }

    /// <summary>
    /// Whether the specified player(s) have killed the given monster(s) between [min count] (default 1) and [max count] (default unlimited) times inclusively.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="monsterNames">The monster names to require.</param>
    /// <param name="minCount">The minimum count of monsters to require (default 1).</param>
    /// <param name="maxCount">The maximum count of monsters to require (default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string KilledMonsters(string player = Current, params string[] monsterNames)
    {
        return $"PLAYER_KILLED_MONSTERS {player} {string.Join(" ", monsterNames)}";
    }

    /// <summary>
    /// Whether the specified player(s) have killed the given monster(s) between [min count] (default 1) and [max count] (default unlimited) times inclusively.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="monsterNames">The monster names to require.</param>
    /// <param name="minCount">The minimum count of monsters to require (default 1).</param>
    /// <param name="maxCount">The maximum count of monsters to require (default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string KilledMonsters(
        int minCount,
        int maxCount,
        string player = Current,
        params string[] monsterNames
    )
    {
        return $"PLAYER_KILLED_MONSTERS {player} {string.Join(" ", monsterNames)} {minCount} {maxCount}";
    }

    /// <summary>
    /// Whether the specified player(s) have shipped the given item between [min count] (default 1) and [max count] (default unlimited) times inclusively.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="itemId">The item ID to require.</param>
    /// <param name="minCount">The minimum count of items to require (default 1).</param>
    /// <param name="maxCount">The maximum count of items to require (default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string ShippedBasicItem(
        string itemId,
        int minCount = 1,
        int maxCount = int.MaxValue,
        string player = Current
    )
    {
        return $"PLAYER_SHIPPED_BASIC_ITEM {player} {itemId} {minCount} {maxCount}";
    }

    /// <summary>
    /// Whether the specified player(s) have the given special order active.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="orderId">The special order ID to require.</param>
    /// <returns>The current query instance.</returns>
    public static string SpecialOrderActive(string orderId, string player = Current)
    {
        return $"PLAYER_SPECIAL_ORDER_ACTIVE {player} {orderId}";
    }

    /// <summary>
    /// Whether the specified player(s) have completed the given special order.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="orderId">The special order ID to require.</param>
    /// <returns>The current query instance.</returns>
    public static string SpecialOrderComplete(string orderId, string player = Current)
    {
        return $"PLAYER_SPECIAL_ORDER_COMPLETE {player} {orderId}";
    }

    /// <summary>
    /// Whether the specified player(s) have a special rule active.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="ruleId">The special rule ID to require.</param>
    /// <returns>The current query instance.</returns>
    public static string SpecialOrderRuleActive(string ruleId, string player = Current)
    {
        return $"PLAYER_SPECIAL_ORDER_RULE_ACTIVE {player} {ruleId}";
    }

    /// <summary>
    /// Whether a stat counter for the specified player(s) has a value between <min value> and [max value] (default unlimited) inclusively.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="statName">The stat name to require.</param>
    /// <param name="minValue">The minimum value to require.</param>
    /// <param name="maxValue">The maximum value to require (default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string Stat(
        string statName,
        int minValue,
        int maxValue = int.MaxValue,
        string player = Current
    )
    {
        return $"PLAYER_STAT {player} {statName} {minValue} {maxValue}";
    }

    /// <summary>
    /// Whether the specified player(s) have visited one of the given location names.
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <param name="locationNames">The location names to require.</param>
    /// <returns>The current query instance.</returns>
    public static string VisitedLocation(string player, params string[] locationNames)
    {
        return $"PLAYER_VISITED_LOCATION {player} {string.Join(" ", locationNames)}";
    }
}
