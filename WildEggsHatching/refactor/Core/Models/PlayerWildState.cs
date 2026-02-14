using System.Collections.Generic;

namespace WildEggsHatching.Core.Models;

/// <summary>
/// Centralized player progression state for wild eggs.
/// </summary>
public sealed class PlayerWildState
{
    /// <summary>Animal IDs that have been unlocked.</summary>
    public HashSet<string> UnlockedAnimals { get; private set; } = new();

    /// <summary>Animal IDs that have been discovered (hatched but not necessarily unlocked).</summary>
    public HashSet<string> DiscoveredAnimals { get; private set; } = new();

    /// <summary>Note IDs that have been collected.</summary>
    public HashSet<string> CollectedNotes { get; private set; } = new();

    /// <summary>Day when the player started the progression.</summary>
    public int StartDay { get; set; }

    /// <summary>
    /// Tracks when quests were completed.
    /// Key is quest ID, value is the game day when completed.
    /// </summary>
    public Dictionary<string, int> QuestCompletionDays { get; private set; } = new();

    public bool IsUnlocked(string animalId) => UnlockedAnimals.Contains(animalId);

    public bool IsDiscovered(string animalId) => DiscoveredAnimals.Contains(animalId);

    public bool HasCollectedNote(string noteId) => CollectedNotes.Contains(noteId);

    public void Unlock(string animalId)
    {
        if (!UnlockedAnimals.Contains(animalId))
        {
            UnlockedAnimals.Add(animalId);
        }
    }

    public void Discover(string animalId)
    {
        if (!DiscoveredAnimals.Contains(animalId))
        {
            DiscoveredAnimals.Add(animalId);
        }
    }

    public void CollectNote(string noteId)
    {
        if (!CollectedNotes.Contains(noteId))
        {
            CollectedNotes.Add(noteId);
        }
    }

    /// <summary>
    /// Sets the completion day for a quest.
    /// </summary>
    /// <param name="questId">The quest ID.</param>
    /// <param name="day">The game day when the quest was completed.</param>
    public void SetQuestCompletionDay(string questId, int day)
    {
        QuestCompletionDays[questId] = day;
    }

    /// <summary>
    /// Gets the completion day for a quest.
    /// </summary>
    /// <param name="questId">The quest ID.</param>
    /// <returns>The game day when the quest was completed, or null if not completed.</returns>
    public int? GetQuestCompletionDay(string questId)
    {
        return QuestCompletionDays.TryGetValue(questId, out var day) ? day : null;
    }

    /// <summary>
    /// Checks if a quest has been completed.
    /// </summary>
    /// <param name="questId">The quest ID.</param>
    /// <returns>True if the quest has been completed.</returns>
    public bool IsQuestCompleted(string questId)
    {
        return QuestCompletionDays.ContainsKey(questId);
    }
}

