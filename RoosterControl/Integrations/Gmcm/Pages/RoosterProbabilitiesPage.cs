using Mikew.NestCore.Integrations.Gmcm;
using RoosterControl.Framework.Config;

namespace RoosterControl.Integrations.Gmcm.Pages;

internal static class RoosterProbabilitiesPage
{
    public static void Build(GmcmPageBuilder<RoosterControlConfig> b)
    {
        b.Section("probabilities.eggs");
        b.Bool(cfg => cfg.General.EnableEggProbability, optionId: "prob.enableEggProbability", tooltip: true);
        b.Int(cfg => cfg.General.EggProbability, optionId: "prob.eggProbability", tooltip: true, min: 0, max: 100, interval: 1);

        b.Bool(cfg => cfg.General.EnableLargeEggProbability, optionId: "prob.enableLargeEggProbability", tooltip: true);
        b.Int(cfg => cfg.General.LargeEggProbability, optionId: "prob.largeEggProbability", tooltip: true, min: 0, max: 100, interval: 1);

        b.Int(cfg => cfg.General.FertilizedEggProbability, optionId: "prob.fertilizedEggProbability", tooltip: true, min: 0, max: 100, interval: 1);

        b.Separator(simple: true);

        b.Section("probabilities.breeding");
        b.Int(cfg => cfg.General.RoosterToChickenRatio, optionId: "prob.roosterToChickenRatio", tooltip: true, min: 0, max: 100, interval: 1);
        b.Float(cfg => cfg.General.BoostByRooster, optionId: "prob.boostByRooster", tooltip: true, min: 0, max: 10, interval: 0.05f);
        b.Int(cfg => cfg.General.ChosenPercentageDailyLuckBonus, optionId: "prob.chosenDailyLuckBonusPct", tooltip: true, min: 0, max: 100, interval: 1);
    }
}

