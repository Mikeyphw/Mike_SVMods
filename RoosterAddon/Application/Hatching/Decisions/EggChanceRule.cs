namespace RoosterAddon.Application.Hatching.Decisions;

/// <summary>
/// Pure inputs for deciding whether an egg is assigned due to a chance roll.
/// </summary>
internal sealed record EggChanceRule(
    bool Enabled,
    int ChancePercent,
    string EggId,
    string? FemaleEggIdToRemove
);
