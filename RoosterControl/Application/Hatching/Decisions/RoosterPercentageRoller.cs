using RoosterControl.Helpers;

namespace RoosterControl.Application.Hatching.Decisions;

/// <summary>
/// Default chance roller using RoosterControl's percentage logic.
/// </summary>
internal sealed class RoosterPercentageRoller : IPercentageRoller
{
    public bool Roll(int chancePercent) => Helpers.RoosterHelper.UpdatePercentage(chancePercent);

    public bool Roll(float chancePercent) => Helpers.RoosterHelper.UpdatePercentage(chancePercent);
}
