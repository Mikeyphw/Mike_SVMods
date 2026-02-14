namespace Mikew.BirdsOfAFeather.Helpers.GameStateQueries;

public static class Randomization
{
    /// <summary>
    /// A random probability check which is re-rolled each time it's called.
    /// </summary>
    /// <param name="chance">The probability of the check being true.</param>
    /// <param name="addDailyLuck">Whether to add the current player's daily luck to the probability.</param>
    /// <returns>The current query instance.</returns>
    public static string Random(double chance, bool addDailyLuck = false)
    {
        string luckString = addDailyLuck ? "@addDailyLuck" : "";
        return$"RANDOM {chance} {luckString}";
        
    }

    /// <summary>
    /// Choose a random integer between <min> and <max> inclusively, and check whether it matches one of the <choices>.
    /// </summary>
    /// <param name="interval">The interval for which the result will be identical (one of tick, day, season, or year).</param>
    /// <param name="key">The key for which the result will be identical.</param>
    /// <param name="min">The minimum value of the random integer.</param>
    /// <param name="max">The maximum value of the random integer.</param>
    /// <param name="choices">The choices to check against.</param>
    /// <returns>The current query instance.</returns>
    public static string SyncedChoice(string interval, string key, int min, int max, params int[] choices)
    {
        return$"SYNCED_CHOICE {interval} {key} {min} {max} {string.Join(" ", choices)}";
        
    }

    /// <summary>
    /// A random probability check. The result will be identical for all queries with the same <key> value during the given <interval>.
    /// </summary>
    /// <param name="interval">The interval for which the result will be identical (one of tick, day, season, or year).</param>
    /// <param name="key">The key for which the result will be identical.</param>
    /// <param name="chance">The probability of the check being true.</param>
    /// <param name="addDailyLuck">Whether to add the current player's daily luck to the probability.</param>
    /// <returns>The current query instance.</returns>
    public static string SyncedRandom(string interval, string key, double chance, bool addDailyLuck = false)
    {
        string luckString = addDailyLuck ? "@addDailyLuck" : "";
        return$"SYNCED_RANDOM {interval} {key} {chance} {luckString}";
        
    }

    /// <summary>
    /// A specialized variant of SYNCED_DAY_RANDOM used to calculate the chance of rain in summer, which increases with the day number.
    /// </summary>
    /// <param name="baseChance">The base probability of the check being true.</param>
    /// <param name="dayMultiplier">The multiplier for the day number.</param>
    /// <returns>The current query instance.</returns>
    public static string SyncedSummerRainRandom(double baseChance, double dayMultiplier)
    {
        return$"SYNCED_SUMMER_RAIN_RANDOM {baseChance} {dayMultiplier}";
        
    }
}