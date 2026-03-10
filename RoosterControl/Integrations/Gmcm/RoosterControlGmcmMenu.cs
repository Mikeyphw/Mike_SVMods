using System;
using Mikew.NestCore.Integrations.Gmcm;
using RoosterControl.Framework.Config;
using RoosterControl.Application;
using RoosterControl.Integrations.Gmcm.Pages;

namespace RoosterControl.Integrations.Gmcm;

internal sealed class RoosterControlGmcmMenu
{
    private readonly RoosterCoordinator _owner;
    private readonly Action<GmcmPageBuilder<RoosterControlConfig>> _buildCompatPage;

    public RoosterControlGmcmMenu(
        RoosterCoordinator owner,
        Action<GmcmPageBuilder<RoosterControlConfig>> buildCompatPage)
    {
        _owner = owner;
        _buildCompatPage = buildCompatPage;
    }

    public void DefineLayout(GmcmMenuLayout<RoosterControlConfig> layout)
    {
        layout.Root(root => RoosterRootPage.Build(root, _owner));
        layout.Page("compat", _buildCompatPage);
        layout.Page("probabilities", RoosterProbabilitiesPage.Build);
        layout.Page("debug", RoosterDebugPage.Build);
        RoosterTypePages.Define(layout, _owner);
    }
}
