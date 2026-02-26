using static LetMeHatch.EnumsAndStrings.Enums;

namespace LetMeHatch.Core.ClonePipeline;

/// <summary>
/// Declarative plan item describing a single produce item clone for an animal.
/// </summary>
public sealed record CloneSpec(
    string AnimalKey,
    string OriginalItemId,
    string CloneItemId,
    CloneItemType CloneItemType,
    ItemType ItemType,
    bool IsFertilized,
    string ColorSeedKey
);
