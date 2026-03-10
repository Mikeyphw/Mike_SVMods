using System;
using System.Collections.Generic;
using Mikew.NestCore.Helpers.Logging;
using NSubstitute;
using RoosterControl.Application.Compat;
using RoosterControl.Integrations.StardewUi;
using RoosterControl.Integrations.StardewUi.Compat;
using RoosterControl.Integrations.StardewUi.Produce;
using StardewModdingAPI;
using StardewValley.GameData.FarmAnimals;
using Xunit;

namespace RoosterControl.Tests;

public sealed class RoosterEditorHubViewModelTests
{
    [Fact]
    public void InitialTab_UsesProduceWhenRequested()
    {
        var viewModel = CreateHub(RoosterEditorHubTab.Produce);

        Assert.True(viewModel.IsProduceTab);
        Assert.Equal("[Produce]", viewModel.ProduceTabText);
    }

    private static RoosterEditorHubViewModel CreateHub(RoosterEditorHubTab tab)
    {
        var config = new Framework.Config.RoosterControlConfig();

        return new RoosterEditorHubViewModel(
            new RoosterCompatEditorViewModel(CreateCompatService(new CompatSelectionState()), new CompatSelectionState(), CreateAnimalData, () => { }),
            new RoosterProduceEditorViewModel(new RoosterProduceEditorService(), config, () => { }, Enums.RoosterType.White, RoosterProduceSection.Normal),
            new RoosterSettingsEditorViewModel(config, () => { }, () => string.Empty, () => string.Empty),
            new RoosterDebugEditorViewModel(config, () => { }),
            tab);
    }

    private static RoosterCompatEditorService CreateCompatService(CompatSelectionState state)
    {
        var helper = Substitute.For<IModHelper>();
        var content = Substitute.For<IGameContentHelper>();
        var manifest = Substitute.For<IManifest>();
        var log = Substitute.For<ILog>();

        manifest.UniqueID.Returns("Mikew.RoosterControl.Tests");
        helper.GameContent.Returns(content);

        content.Load<EggSourcesCatalog>("Mods/Mikew.RoosterControl.Tests/compat/egg_sources").Returns(new EggSourcesCatalog());
        content.Load<RoosterSourcesCatalog>("Mods/Mikew.RoosterControl.Tests/compat/rooster_sources").Returns(new RoosterSourcesCatalog());

        var service = new CompatSelectionService(helper, manifest, log);
        service.InitializeOrRepair(state, out _);
        return new RoosterCompatEditorService(service);
    }

    private static IDictionary<string, FarmAnimalData> CreateAnimalData()
        => new Dictionary<string, FarmAnimalData>(StringComparer.OrdinalIgnoreCase);
}
