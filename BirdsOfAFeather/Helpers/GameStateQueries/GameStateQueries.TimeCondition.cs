namespace Mikew.BirdsOfAFeather.Helpers.GameStateQueries;

/// <summary>
/// A query condition related to time.
/// </summary>
public static class TimeCondition
{
    /// <summary>
    /// The day of month.
    /// </summary>
    /// <param name="days">One or more days of the month (1-28, or "even" or "odd").</param>
    /// <returns>The current query instance.</returns>
    public static string DayOfMonth(params string[] days)
    {
        return $"DAY_OF_MONTH {string.Join(" ", days)}";
    }

    /// <summary>
    /// The day of week.
    /// </summary>
    /// <param name="days">One or more days of the week (0-6, or three-letter English name, or full English name).</param>
    /// <returns>The current query instance.</returns>
    public static string DayOfWeek(params string[] days)
    {
        return $"DAY_OF_WEEK {string.Join(" ", days)}";
    }

    /// <summary>
    /// The number of days played.
    /// </summary>
    /// <param name="min">The minimum number of days played.</param>
    /// <param name="max">The maximum number of days played (optional, default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string DaysPlayed(int min, int max = int.MaxValue)
    {
        return $"DAYS_PLAYED {min} {max}";
    }

    /// <summary>
    /// Whether it's a festival day.
    /// </summary>
    /// <param name="locationContext">The location context (optional, default "Any").</param>
    /// <param name="dayOffset">The day offset (optional, default 0).</param>
    /// <returns>The current query instance.</returns>
    public static string IsFestivalDay(string locationContext = "Any", int dayOffset = 0)
    {
        return 
            $"IS_FESTIVAL_DAY {(string.IsNullOrEmpty(locationContext) ? "Any" : locationContext)} {dayOffset}"
        ;
    }

    /// <summary>
    /// Whether a passive festival is open today.
    /// </summary>
    /// <param name="id">The ID of the passive festival.</param>
    /// <returns>The current query instance.</returns>
    public static string IsPassiveFestivalOpen(string id)
    {
        return $"IS_PASSIVE_FESTIVAL_OPEN {id}";
    }

    /// <summary>
    /// Whether a passive festival is today.
    /// </summary>
    /// <param name="id">The ID of the passive festival.</param>
    /// <returns>The current query instance.</returns>
    public static string IsPassiveFestivalToday(string id)
    {
        return $"IS_PASSIVE_FESTIVAL_TODAY {id}";
    }

    /// <summary>
    /// The season.
    /// </summary>
    /// <param name="seasons">One or more seasons (spring, summer, fall, or winter).</param>
    /// <returns>The current query instance.</returns>
    public static string Season(params string[] seasons)
    {
        return $"SEASON {string.Join(" ", seasons)}";
    }

    /// <summary>
    /// The season and day.
    /// </summary>
    /// <param name="seasonDays">One or more season and day pairs (e.g. "spring 15", "winter 20").</param>
    /// <returns>The current query instance.</returns>
    public static string SeasonDay(params string[] seasonDays)
    {
        return $"SEASON_DAY {string.Join(" ", seasonDays)}";
    }

    /// <summary>
    /// The current time.
    /// </summary>
    /// <param name="min">The minimum time (in 26-hour format).</param>
    /// <param name="max">The maximum time (in 26-hour format, optional, default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string Time(int min, int max = int.MaxValue)
    {
        return $"TIME {min} {max}";
    }

    /// <summary>
    /// The current year.
    /// </summary>
    /// <param name="min">The minimum year.</param>
    /// <param name="max">The maximum year (optional, default unlimited).</param>
    /// <returns>The current query instance.</returns>
    public static string Year(int min, int max = int.MaxValue)
    {
        return $"YEAR {min} {max}";
    }
}
