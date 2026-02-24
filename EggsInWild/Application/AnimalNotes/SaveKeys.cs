namespace EggsInWild.Application.AnimalNotes;

public static class SaveKeys
{
    public const int Version = 3;
    // SMAPI save-data keys only allow letters/numbers/._-
    public const string AnimalNotes = $"{EggsInWild.Constants.ModId}.AnimalNotes";
}
