namespace LetMeHatch.Core.Cloning;

/// <summary>Parsed clone-id parts.</summary>
public readonly record struct CloneIdParts(string AnimalKey, string OriginalItemId, string Suffix);
