using Mikew.NestCore.Integrations.Gmcm;
using RoosterControl.Framework.Config;

namespace RoosterControl.Integrations.Gmcm.Pages;

internal static class RoosterDebugPage
{
    public static void Build(GmcmPageBuilder<RoosterControlConfig> b)
    {
        b.ParagraphGmcmOptionsStatus();
        b.Separator(simple: true);

        b.Section("debug.flags");
        b.Bool(cfg => cfg.General.EnableDebug, optionId: "debug.enableDebug", tooltip: true);

        b.Separator(simple: true);

        b.Section("debug.logging");
        b.Bool(cfg => cfg.VerboseLogging, optionId: "debug.verboseLogging", tooltip: true);
        b.Bool(cfg => cfg.TestingLogging, optionId: "debug.testingLogging", tooltip: true);

        b.Paragraph("debug.note");

#if RCTRL_DEV
        b.Separator(simple: true);
        b.Section("debug.tuning.luck");
        b.Float(cfg => cfg.DebugKnobs.BonusCalculationMultiplier, optionId: "debug.tuning.bonusCalculation", tooltip: true, min: 0, max: 10, interval: 0.05f);
        b.Float(cfg => cfg.DebugKnobs.SuperUnluckyBonusMultiplier, optionId: "debug.tuning.superUnluckyBonus", tooltip: true, min: -10, max: 0, interval: 0.25f);
        b.Float(cfg => cfg.DebugKnobs.SuperLuckyBonusMultiplier, optionId: "debug.tuning.superLuckyBonus", tooltip: true, min: 0, max: 10, interval: 0.25f);
        b.Float(cfg => cfg.DebugKnobs.LuckAdjustmentOffset, optionId: "debug.tuning.luckAdjustment", tooltip: true, min: -100, max: 100, interval: 1f);
#endif
    }
}

