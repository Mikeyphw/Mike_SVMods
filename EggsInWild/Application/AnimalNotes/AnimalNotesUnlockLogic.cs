using System;
using System.Collections.Generic;
using System.Linq;

namespace EggsInWild.Application.AnimalNotes;

public sealed record FarmerCleanupStatus(long FarmerId, bool QuestGiven, bool QuestCompleted, bool MailReceived);

public static class AnimalNotesUnlockLogic
{
    public static bool IsUnlockMailEligible(int dayNumber, int questCompletedDay, int daysToMature)
        => dayNumber - questCompletedDay >= daysToMature + 2;

    public static int DaysUntilUnlockMailEligible(int dayNumber, int questCompletedDay, int daysToMature)
    {
        var needed = daysToMature + 2;
        var elapsed = dayNumber - questCompletedDay;
        return Math.Max(0, needed - elapsed);
    }

    /// <summary>
    /// Balanced cleanup: keep the record while any farmer is in-progress (quest given but not completed),
    /// and only allow cleanup once at least one completion happened and all completions have received mail.
    /// Farmers who never started don't block cleanup.
    /// </summary>
    public static bool ShouldCleanupBalanced(IEnumerable<FarmerCleanupStatus> farmers)
    {
        var list = (farmers ?? Array.Empty<FarmerCleanupStatus>()).ToList();
        if (list.Count == 0)
            return false;

        var anyCompleted = list.Any(f => f.QuestCompleted);
        if (!anyCompleted)
            return false;

        var anyInProgress = list.Any(f => f.QuestGiven && !f.QuestCompleted);
        if (anyInProgress)
            return false;

        return list.Where(f => f.QuestCompleted).All(f => f.MailReceived);
    }

    public static bool TryMarkQuestCompleted(AnimalNotesFarmerState state, int dayNumber)
    {
        if (state.DayQuestCompleted is not null)
            return false;

        state.DayQuestCompleted = dayNumber;
        return true;
    }

    public static bool TryMarkMailQueued(AnimalNotesFarmerState state, int dayNumber)
    {
        if (state.MailQueued)
            return false;

        state.MailQueued = true;
        state.DayMailQueued ??= dayNumber;
        return true;
    }
}
