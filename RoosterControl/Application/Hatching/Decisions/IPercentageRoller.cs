namespace RoosterControl.Application.Hatching.Decisions;

/// <summary>
/// Rolls percentage chances. Inject a deterministic implementation for tests.
/// </summary>
internal interface IPercentageRoller
{
    bool Roll(int chancePercent);
    bool Roll(float chancePercent);
}
