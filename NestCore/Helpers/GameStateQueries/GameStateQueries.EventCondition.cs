namespace Mikew.NestCore.Helpers.GameStateQueries;

/// <summary>
/// A query condition related to events.
/// </summary>
public static class EventCondition
{
    /// <summary>
    /// Whether the player is viewing any event or attending any active festival.
    /// </summary>
    /// <returns>The current query instance.</returns>
    public const string IsEvent = "IS_EVENT";

    /// <summary>
    /// Whether the player is viewing an event or attending an active festival with a specific ID.
    /// </summary>
    /// <param name="eventIds">One or more event IDs (e.g. "festival_spring13" for the Egg Festival).</param>
    /// <returns>The current query instance.</returns>
    public static string IsEventWithParameter(params string[] eventIds)
    {
        return $"IS_EVENT {string.Join(" ", eventIds)}";
        
    }
}