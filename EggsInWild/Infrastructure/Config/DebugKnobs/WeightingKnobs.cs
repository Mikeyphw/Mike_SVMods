#if WEGG_DEV
namespace EggsInWild.Infrastructure.Config;

public sealed class WeightingKnobs
{
    public int HorizonDays { get; set; } = 7;
    public float DeluxeProbability { get; set; } = 0.1f;

    public float K { get; set; } = 1000.0f;
    public float Alpha { get; set; } = 0.5f;

    public float CapHigh { get; set; } = 8.0f;
    public float CapLow { get; set; } = 20.0f;

    public float UnlockedFactor { get; set; } = 0.85f;
    public float HiddenNoAnchorPenalty { get; set; } = 0.5f;
}
#endif

