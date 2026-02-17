namespace RoosterAddon.Application.Hatching.Decisions;

internal sealed record FertilizedOnlyRule(
    float FemaleUpgradeChancePercent,
    int RoosterToChickenRatioPercent,
    string RoosterFertilizedEggId,
    string FemaleFertilizedEggId
);
