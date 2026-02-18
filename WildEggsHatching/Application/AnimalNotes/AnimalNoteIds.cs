using System;

namespace WildEggsHatching.Application.AnimalNotes;

public static class AnimalNoteIds
{
    public static string NoteItemId(string farmAnimalName)
        => $"{WildEggsHatching.Constants.ModId}_junimoNote_{NormalizeToken(farmAnimalName)}";

    public static string NoteQuestId(string farmAnimalName)
        => $"wegg_{NormalizeToken(farmAnimalName)}Note";

    public static string NoteMailId(string farmAnimalName)
        => $"wegg_NoteMail_{NormalizeToken(farmAnimalName)}";

    private static string NormalizeToken(string s)
        => (s ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal);
}
