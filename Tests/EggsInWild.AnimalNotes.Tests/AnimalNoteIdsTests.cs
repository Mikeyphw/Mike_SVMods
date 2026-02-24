using EggsInWild.Application.AnimalNotes;
using Xunit;

namespace EggsInWild.AnimalNotes.Tests;

public sealed class AnimalNoteIdsTests
{
    [Theory]
    [InlineData("Blue Chicken", "Mikew.eggsinwild_junimoNote_BlueChicken", "wegg_BlueChickenNote", "wegg_NoteMail_BlueChicken")]
    [InlineData("  Void  Chicken ", "Mikew.eggsinwild_junimoNote_VoidChicken", "wegg_VoidChickenNote", "wegg_NoteMail_VoidChicken")]
    public void GeneratesStableIds(string token, string expectedItem, string expectedQuest, string expectedMail)
    {
        Assert.Equal(expectedItem, AnimalNoteIds.NoteItemId(token));
        Assert.Equal(expectedQuest, AnimalNoteIds.NoteQuestId(token));
        Assert.Equal(expectedMail, AnimalNoteIds.NoteMailId(token));
    }
}
