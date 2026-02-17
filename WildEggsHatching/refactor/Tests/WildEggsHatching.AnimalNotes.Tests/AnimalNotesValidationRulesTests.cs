using System.Collections.Generic;
using WildEggsHatching.Application.AnimalNotes;
using Xunit;

namespace WildEggsHatching.AnimalNotes.Tests;

public sealed class AnimalNotesValidationRulesTests
{
    [Fact]
    public void FiltersExpectedTokensAgainstFarmAnimalKeys()
    {
        var wild = new[] { "Chicken", "Duck", "NotInGame", "Chicken" };
        var farm = new[] { "Chicken", "Duck" };

        var tokens = AnimalNotesValidationRules.GetExpectedAnimalTokens(wild, farm);

        Assert.Equal(new List<string> { "Chicken", "Duck" }, tokens);
    }

    [Fact]
    public void AnalyzeFindsMissingAndExtra()
    {
        var tokens = new[] { "Chicken", "Duck" };

        var presentNotes = new[] { AnimalNoteIds.NoteItemId("Chicken") }; // Duck missing
        var presentQuests = new[] { AnimalNoteIds.NoteQuestId("Chicken"), AnimalNoteIds.NoteQuestId("Duck"), "wildEggs_ExtraNote" };
        var presentMail = new[] { AnimalNoteIds.NoteMailId("Chicken"), AnimalNoteIds.NoteMailId("Duck"), "wildEggs_NoteMail_Extra" };

        var result = AnimalNotesValidationRules.Analyze(tokens, presentNotes, presentQuests, presentMail);

        Assert.False(result.IsOk);
        Assert.Contains(AnimalNoteIds.NoteItemId("Duck"), result.MissingNotes);
        Assert.Contains("wildEggs_ExtraNote", result.ExtraQuests);
        Assert.Contains("wildEggs_NoteMail_Extra", result.ExtraMail);
    }
}
