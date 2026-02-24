using System;

namespace RoosterControl.Application.Hatching.Decisions;

/// <summary>
/// Pure decision functions for hatching logic (no SMAPI types).
/// </summary>
internal static class HatchingDecisionEngine
{
    public static EggChanceDecision? DecideEggChance(EggChanceRule rule, IPercentageRoller roller)
    {
        if (rule is null)
            throw new ArgumentNullException(nameof(rule));
        if (roller is null)
            throw new ArgumentNullException(nameof(roller));

        if (!rule.Enabled)
            return null;

        if (string.IsNullOrWhiteSpace(rule.EggId))
            return null;

        if (!roller.Roll(rule.ChancePercent))
            return null;

        return new EggChanceDecision(
            EggIdToAddToRooster: rule.EggId,
            FemaleEggIdToRemove: rule.FemaleEggIdToRemove
        );
    }

    public static FertilizedOnlyDecision DecideFertilizedOnly(FertilizedOnlyRule rule, IPercentageRoller roller)
    {
        if (rule is null)
            throw new ArgumentNullException(nameof(rule));
        if (roller is null)
            throw new ArgumentNullException(nameof(roller));

        var upgradeFemale = roller.Roll(rule.FemaleUpgradeChancePercent);
        var roosterHatches = roller.Roll(rule.RoosterToChickenRatioPercent);

        return new FertilizedOnlyDecision(
            UpgradeFemaleProduceToFertilized: upgradeFemale,
            RoosterHatchesFertilizedEgg: roosterHatches,
            RoosterEggId: roosterHatches ? rule.RoosterFertilizedEggId : null,
            FemaleEggId: roosterHatches ? null : rule.FemaleFertilizedEggId
        );
    }
}
