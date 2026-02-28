using System;

namespace RoosterControl.Application.Planning;

/// <summary>
/// Immutable snapshot of daily farm-animal decisions computed by the coordinator and applied by the asset editor.
/// </summary>
internal sealed record DailyFarmAnimalsDecisions(
    int Day,
    long FarmId,
    string Fingerprint,
    DailyHatchingPlanEnvelope? Plan
)
{
    public bool MatchesToday(long farmId, int day)
        => FarmId == farmId && Day == day;
}
