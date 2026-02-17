namespace RoosterAddon.Application.Hatching.Decisions;

internal sealed record FertilizedOnlyDecision(
    bool UpgradeFemaleProduceToFertilized,
    bool RoosterHatchesFertilizedEgg,
    string? RoosterEggId,
    string? FemaleEggId
);
