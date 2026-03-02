using System.Collections.Generic;

namespace RoosterControl.Application.Compat;

/// <summary>
/// Per-save compat selection state.
/// This stores the user's chosen egg/rooster sources, independent of load order.
/// </summary>
internal sealed class CompatSelectionState
{
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Selected egg source per logical egg kind.</summary>
    public Dictionary<EggKind, string> SelectedEggSources { get; set; } = new();

    /// <summary>Enabled rooster providers (unioned).</summary>
    public List<string> EnabledRoosterSources { get; set; } = new();
}
