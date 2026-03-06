using System.Collections.Generic;
using RoosterControl.Application.Compat;

namespace RoosterControl.Integrations.StardewUi.Compat;

/// <summary>
/// Immutable snapshot of the current compat editor state for UI display.
/// </summary>
public sealed record RoosterCompatEditorSnapshot(
    IReadOnlyList<RoosterCompatEggGroupRecord> EggGroups,
    IReadOnlyList<RoosterCompatProviderRecord> ProviderRecords
);

/// <summary>
/// Immutable record describing one egg category section and its kinds.
/// </summary>
public sealed record RoosterCompatEggGroupRecord(
    string Category,
    string Section,
    IReadOnlyList<RoosterCompatEggKindRecord> Kinds
);

/// <summary>
/// Immutable record describing one egg kind and its available source choices.
/// </summary>
public sealed record RoosterCompatEggKindRecord(
    EggKind Kind,
    string DisplayName,
    IReadOnlyList<string> AvailableSources,
    string? SelectedSourceId,
    string? ValidationNote
);

/// <summary>
/// Immutable record describing one rooster provider choice.
/// </summary>
public sealed record RoosterCompatProviderRecord(
    string ProviderId,
    string DisplayName,
    bool IsEnabled,
    string? RoosterEggItemId
);

/// <summary>
/// Optional validation payload for callers that want a structured validation result.
/// </summary>
public sealed record RoosterCompatValidationRecord(
    string SummaryText,
    int WarningCount,
    int ErrorCount
);

/// <summary>
/// Mutable draft for applying changes back to compat selections.
/// </summary>
public sealed class RoosterCompatEditorDraft
{
    /// <summary>Egg source selections to apply. Null means no change.</summary>
    public Dictionary<EggKind, string>? EggSelections { get; set; }

    /// <summary>Whether to use all providers. Null means no change.</summary>
    public bool? UseAllProviders { get; set; }

    /// <summary>Specific provider to select when UseAllProviders is false.</summary>
    public string? SelectedProviderId { get; set; }
}
