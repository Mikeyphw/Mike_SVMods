namespace EggsInWild.Core.Weighting;

/// <summary>
/// Canonical defaults for the economy-based weighting system (docs/DesignNotes.md).
/// </summary>
public static class WeggWeightingDefaults
{
    public const int HorizonDays = 7;
    public const double DeluxeProbability = 0.1;

    public const double K = 1000;
    public const double Alpha = 0.5;

    public const double CapHigh = 8;
    public const double CapLow = 20;

    public const double UnlockedFactor = 0.85;

    /// <summary>
    /// Optional penalty applied when an animal cannot be anchored to a shop listing price.
    /// </summary>
    public const double HiddenNoAnchorPenalty = 0.5;
}
