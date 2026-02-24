namespace EggsInWild.Application.Progression;

public static class SaveKeys
{
    public const int Version = 1;
    // SMAPI save-data keys only allow letters/numbers/._-
    public const string Progression = $"{EggsInWild.Constants.ModId}.Progression";
}
