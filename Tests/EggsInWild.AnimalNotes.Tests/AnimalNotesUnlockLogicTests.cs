using System.Collections.Generic;
using EggsInWild.Application.AnimalNotes;
using Xunit;

namespace EggsInWild.AnimalNotes.Tests;

public sealed class AnimalNotesUnlockLogicTests
{
    [Fact]
    public void UnlockEligibilityUsesDaysToMaturePlusTwo()
    {
        // daysToMature=3 => needed=5
        Assert.False(AnimalNotesUnlockLogic.IsUnlockMailEligible(dayNumber: 9, questCompletedDay: 5, daysToMature: 3));
        Assert.True(AnimalNotesUnlockLogic.IsUnlockMailEligible(dayNumber: 10, questCompletedDay: 5, daysToMature: 3));
    }

    [Fact]
    public void CleanupBalancedRequiresNoInProgressAndAllCompletedHaveMail()
    {
        var farmers = new List<FarmerCleanupStatus>
        {
            new(FarmerId: 1, QuestGiven: true, QuestCompleted: true, MailReceived: true),
            new(FarmerId: 2, QuestGiven: false, QuestCompleted: false, MailReceived: false),
        };

        Assert.True(AnimalNotesUnlockLogic.ShouldCleanupBalanced(farmers));

        farmers[0] = farmers[0] with { MailReceived = false };
        Assert.False(AnimalNotesUnlockLogic.ShouldCleanupBalanced(farmers));

        farmers[0] = farmers[0] with { MailReceived = true };
        farmers[1] = farmers[1] with { QuestGiven = true, QuestCompleted = false };
        Assert.False(AnimalNotesUnlockLogic.ShouldCleanupBalanced(farmers));
    }

    [Fact]
    public void MarkQuestCompletedIsIdempotent()
    {
        var s = new AnimalNotesFarmerState();
        Assert.True(AnimalNotesUnlockLogic.TryMarkQuestCompleted(s, 123));
        Assert.False(AnimalNotesUnlockLogic.TryMarkQuestCompleted(s, 124));
        Assert.Equal(123, s.DayQuestCompleted);
    }

    [Fact]
    public void MarkMailQueuedIsIdempotent()
    {
        var s = new AnimalNotesFarmerState();
        Assert.True(AnimalNotesUnlockLogic.TryMarkMailQueued(s, 55));
        Assert.False(AnimalNotesUnlockLogic.TryMarkMailQueued(s, 56));
        Assert.True(s.MailQueued);
        Assert.Equal(55, s.DayMailQueued);
    }
}
