using System;
using System.Collections.Generic;
using System.Linq;

namespace WildEggsHatching.Application.AnimalNotes;

public sealed record AnimalNotesValidationResult(
    int AnimalCount,
    List<string> MissingNotes,
    List<string> MissingQuests,
    List<string> MissingMail,
    List<string> ExtraNotes,
    List<string> ExtraQuests,
    List<string> ExtraMail)
{
    public bool IsOk
        => MissingNotes.Count == 0 && MissingQuests.Count == 0 && MissingMail.Count == 0
           && ExtraNotes.Count == 0 && ExtraQuests.Count == 0 && ExtraMail.Count == 0;
}

public static class AnimalNotesValidationRules
{
    public static List<string> GetExpectedAnimalTokens(IEnumerable<string> wildAnimalKeys, IEnumerable<string> farmAnimalKeys)
    {
        var farmSet = new HashSet<string>(farmAnimalKeys ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        return (wildAnimalKeys ?? Array.Empty<string>())
            .Where(k => !string.IsNullOrWhiteSpace(k) && farmSet.Contains(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static AnimalNotesValidationResult Analyze(
        IEnumerable<string> expectedTokens,
        IEnumerable<string> presentNoteIds,
        IEnumerable<string> presentQuestIds,
        IEnumerable<string> presentMailIds)
    {
        var tokens = (expectedTokens ?? Array.Empty<string>())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var expectedNoteIds = tokens.Select(AnimalNoteIds.NoteItemId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var expectedQuestIds = tokens.Select(AnimalNoteIds.NoteQuestId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var expectedMailIds = tokens.Select(AnimalNoteIds.NoteMailId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var notes = (presentNoteIds ?? Array.Empty<string>()).Where(IsJunimoNoteId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var quests = (presentQuestIds ?? Array.Empty<string>()).Where(IsNoteQuestId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var mail = (presentMailIds ?? Array.Empty<string>()).Where(IsNoteMailId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new AnimalNotesValidationResult(
            AnimalCount: tokens.Count,
            MissingNotes: expectedNoteIds.Except(notes).ToList(),
            MissingQuests: expectedQuestIds.Except(quests).ToList(),
            MissingMail: expectedMailIds.Except(mail).ToList(),
            ExtraNotes: notes.Except(expectedNoteIds).ToList(),
            ExtraQuests: quests.Except(expectedQuestIds).ToList(),
            ExtraMail: mail.Except(expectedMailIds).ToList()
        );
    }

    public static bool IsJunimoNoteId(string key)
        => !string.IsNullOrWhiteSpace(key)
           && key.StartsWith($"{Constants.ModId}_junimoNote_", StringComparison.OrdinalIgnoreCase);

    public static bool IsNoteQuestId(string key)
        => !string.IsNullOrWhiteSpace(key)
           && key.StartsWith("wegg_", StringComparison.OrdinalIgnoreCase)
           && key.EndsWith("Note", StringComparison.OrdinalIgnoreCase);

    public static bool IsNoteMailId(string key)
        => !string.IsNullOrWhiteSpace(key)
           && key.StartsWith("wegg_NoteMail_", StringComparison.OrdinalIgnoreCase);
}
