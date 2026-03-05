using Mikew.NestCore.Integrations.Gmcm;
using RoosterControl.Framework.Config;

namespace RoosterControl.Integrations.Gmcm.Pages;

internal static class RoosterRootPage
{
    public static void Build(GmcmPageBuilder<RoosterControlConfig> b)
    {
        b.Section("root.general");
        b.Enum(cfg => cfg.General.UpdateRate, optionId: "general.updateRate", tooltip: true);
        b.Enum(cfg => cfg.General.HatchingMode, optionId: "general.hatchingMode", tooltip: true);
        b.Enum(cfg => cfg.General.HarvestType, optionId: "general.harvestType", tooltip: true);

        b.Separator(simple: true);

        b.Section("root.pages");
        b.Links(
            tooltip: true,
            "compat",
            "probabilities",
            "females",
            "debug"
        );

        b.Separator(simple: true);

        b.Section("root.roosters");
        b.Links(
            tooltip: true,
            "rooster.white",
            "rooster.brown",
            "rooster.blue",
            "rooster.void",
            "rooster.gold"
        );
    }
}

