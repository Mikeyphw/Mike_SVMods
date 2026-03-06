using Mikew.NestCore.Integrations.Gmcm;
using RoosterControl.Application;
using RoosterControl.Framework.Config;

namespace RoosterControl.Integrations.Gmcm.Pages;

internal static class RoosterRootPage
{
    public static void Build(GmcmPageBuilder<RoosterControlConfig> b, RoosterCoordinator owner)
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
        b.Action(
            "compat.openStardewUi",
            onAction: owner.TryOpenCompatEditor,
            tooltip: true,
            isEnabled: owner.HasStardewUiAvailable
        );
        b.ParagraphDynamic(() => owner.GetCompatEditorAvailabilityText(), styled: true, id: "compat_openStardewUi_status");

        b.Separator(simple: true);

        b.Section("root.roosters");
        b.Links(tooltip: true, RoosterTypePages.PageIds);
    }
}
