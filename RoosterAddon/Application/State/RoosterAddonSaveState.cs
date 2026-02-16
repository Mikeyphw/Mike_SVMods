using LuckyTypes = Mikew.BirdsOfAFeather.EnumsAndStrings.Enums.LuckyTypes;

namespace RoosterAddon.Application.State;

/// <summary>
/// Per-save, persisted coordinator state.
/// Phase 6 will extend this to include the deterministic daily plan.
/// </summary>
internal sealed class RoosterAddonSaveState
{
    public int SchemaVersion { get; set; } = 1;

    public int LastRoosterSetupDay { get; set; } = -1;

    public int LastDebugDumpDay { get; set; } = -1;

    public LuckyTypes LastKnownLuck { get; set; } = LuckyTypes.Neutral;
}