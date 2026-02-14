using System;
using System.Collections.Generic;
using StardewValley;
using WildEggsHatching.Core.Models;
using WildEggsHatching.Core.Enums;

namespace WildEggsHatching.Progression.Rules;

/// <summary>
/// Interface for checking quest completion days.
/// </summary>
public interface IQuestCompletionDayProvider
{
    int? GetQuestCompletionDay(string questId);
}

/// <summary>
/// Interface for checking mail.
/// </summary>
public interface IMailChecker
{
    bool HasMail(string mailId);
}

/// <summary>
/// Interface for checking quest completion.
/// </summary>
public interface IQuestChecker
{
    bool HasCompletedQuest(string questId);
}

/// <summary>
/// Contains all context information needed to evaluate unlock rules.
/// This object aggregates data from various sources that rules may need.
/// All game state access goes through injected interfaces for testability.
/// </summary>
public sealed class UnlockContext
{
    /// <summary>The current game day.</summary>
    public int CurrentDay { get; init; }

    /// <summary>The current progression step.</summary>
    public ProgressionStep CurrentStep { get; init; }

    /// <summary>The player's wild animal state.</summary>
    public PlayerWildState PlayerState { get; init; } = null!;

    /// <summary>The game location (for location-specific rules).</summary>
    public GameLocation? Location { get; init; }

    /// <summary>Provider for quest completion day lookups.</summary>
    public IQuestCompletionDayProvider? QuestCompletionDayProvider { get; init; }

    /// <summary>Checker for mail status.</summary>
    public IMailChecker? MailChecker { get; init; }

    /// <summary>Checker for quest completion.</summary>
    public IQuestChecker? QuestChecker { get; init; }

    /// <summary>
    /// Gets the completion day for a specific quest.
    /// Returns null if quest was never completed.
    /// </summary>
    public int? GetQuestCompletionDay(string questId)
    {
        return QuestCompletionDayProvider?.GetQuestCompletionDay(questId);
    }

    /// <summary>
    /// Checks if a specific mail has been received.
    /// </summary>
    public bool HasMail(string mailId)
    {
        return MailChecker?.HasMail(mailId) ?? false;
    }

    /// <summary>
    /// Checks if a specific quest has been completed.
    /// </summary>
    public bool HasCompletedQuest(string questId)
    {
        return QuestChecker?.HasCompletedQuest(questId) ?? false;
    }

    /// <summary>
    /// Gets the number of unlocked animals.
    /// </summary>
    public int UnlockedAnimalCount => PlayerState?.UnlockedAnimals.Count ?? 0;

    /// <summary>
    /// Gets the number of discovered animals.
    /// </summary>
    public int DiscoveredAnimalCount => PlayerState?.DiscoveredAnimals.Count ?? 0;

    /// <summary>
    /// Checks if a specific animal is unlocked.
    /// </summary>
    public bool IsAnimalUnlocked(string animalId)
    {
        return PlayerState?.IsUnlocked(animalId) ?? false;
    }

    /// <summary>
    /// Gets the day count since a reference day.
    /// </summary>
    public int DaysSince(int referenceDay)
    {
        return CurrentDay - referenceDay;
    }

    /// <summary>
    /// Gets the day count since an event occurred.
    /// </summary>
    public int DaysSinceEvent(int eventDay)
    {
        if (eventDay <= 0)
        {
            return 0;
        }

        return CurrentDay - eventDay;
    }

    /// <summary>
    /// Gets the number of days since a quest was completed.
    /// </summary>
    public int DaysSinceQuestCompletion(string questId)
    {
        var completionDay = GetQuestCompletionDay(questId);
        return completionDay.HasValue ? DaysSinceEvent(completionDay.Value) : int.MaxValue;
    }
}

