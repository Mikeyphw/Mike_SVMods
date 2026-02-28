using RoosterControl.Application.Planning;
using LuckyTypes = Mikew.NestCore.EnumsAndStrings.Enums.LuckyTypes;

namespace RoosterControl.Application.State;

/// <summary>
/// Per-save, persisted coordinator state.
/// Stores a minimal "previous vs current" daily decisions history to support debug diffs across sessions.
/// </summary>
internal sealed class RoosterControlSaveState
{
    public int SchemaVersion { get; set; } = 2;

    public int LastRoosterSetupDay { get; set; } = -1;

    public int LastDebugDumpDay { get; set; } = -1;

    public LuckyTypes LastKnownLuck { get; set; } = LuckyTypes.Neutral;

    public DailyFarmAnimalsDecisions? CurrentDailyDecisions { get; set; }

    public DailyFarmAnimalsDecisions? PreviousDailyDecisions { get; set; }
}
