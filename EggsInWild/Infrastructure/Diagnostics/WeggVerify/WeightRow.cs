namespace EggsInWild.Infrastructure.Diagnostics;

public record WeightRow(
    string Id,
    double EffectiveValue,
    double RawWeight,
    double ClampedWeight,
    double FinalWeight,
    bool HiddenNoAnchorPenaltyApplied,
    bool UnlockedPenaltyApplied)
{
    public string Render()
    {
        var flags = new List<string>();
        if (HiddenNoAnchorPenaltyApplied)
            flags.Add("hidden-no-anchor");
        if (UnlockedPenaltyApplied)
            flags.Add("unlocked");

        var flagText = flags.Count == 0 ? "-" : string.Join("|", flags);

        return $"{Id}: v={(int)System.Math.Round(EffectiveValue)}g w={FinalWeight:0.###E+0} raw={RawWeight:0.###E+0} clamp={ClampedWeight:0.###E+0} flags={flagText}";
    }
}

