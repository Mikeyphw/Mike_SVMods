namespace RoosterControl.Application.Hatching.Decisions;

/// <summary>
/// Pure outputs from a successful egg chance decision.
/// </summary>
internal sealed record EggChanceDecision(
    string EggIdToAddToRooster,
    string? FemaleEggIdToRemove
);
