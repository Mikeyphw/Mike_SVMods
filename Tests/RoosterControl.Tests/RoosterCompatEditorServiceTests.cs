using System.Collections.Generic;
using System.Linq;
using Mikew.NestCore.Helpers.Logging;
using NSubstitute;
using RoosterControl.Application.Compat;
using RoosterControl.Integrations.StardewUi.Compat;
using StardewModdingAPI;
using StardewValley.GameData.FarmAnimals;
using Xunit;

namespace RoosterControl.Tests;

public sealed class RoosterCompatEditorServiceTests
{
    [Fact]
    public void LoadSnapshot_BuildsExpectedEggGroupsAndSortedProviders()
    {
        var state = new CompatSelectionState();
        var selectionService = CreateSelectionService();
        selectionService.InitializeOrRepair(state, out _);

        var service = new RoosterCompatEditorService(selectionService);

        state.SelectedEggSources[EggKind.BrownEgg] = "b";
        state.EnabledRoosterSources = new List<string> { "beta" };

        var snapshot = service.LoadSnapshot(state, CreateAnimalData());

        Assert.Equal(4, snapshot.EggGroups.Count);
        Assert.Equal("Standard Eggs", snapshot.EggGroups[0].Category);
        Assert.Contains(snapshot.EggGroups[0].Kinds, k => k.Kind == EggKind.BrownEgg && k.SelectedSourceId == "b");
        Assert.Equal(new[] { "alpha", "beta" }, snapshot.ProviderRecords.Select(p => p.ProviderId).ToArray());
    }

    [Fact]
    public void BuildSummaryText_IncludesResolvedEggsAndRoosterProviderSummary()
    {
        var state = new CompatSelectionState();
        var selectionService = CreateSelectionService();
        selectionService.InitializeOrRepair(state, out _);

        var service = new RoosterCompatEditorService(selectionService);

        state.SelectedEggSources[EggKind.BrownEgg] = "b";
        state.EnabledRoosterSources = new List<string> { "alpha" };

        var summary = service.BuildSummaryText(state, CreateAnimalData());

        Assert.Contains("BrownEgg=180", summary);
        Assert.Contains("roosters=providers:1 keys:1", summary);
    }

    [Fact]
    public void ApplyDraft_UpdatesEggSelectionAndSingleProvider()
    {
        var state = new CompatSelectionState();
        var selectionService = CreateSelectionService();
        selectionService.InitializeOrRepair(state, out _);

        var service = new RoosterCompatEditorService(selectionService);

        var draft = new RoosterCompatEditorDraft
        {
            EggSelections = new Dictionary<EggKind, string>
            {
                [EggKind.BrownEgg] = "a",
            },
            UseAllProviders = false,
            SelectedProviderId = "alpha",
        };

        service.ApplyDraft(state, draft);

        Assert.Equal("a", state.SelectedEggSources[EggKind.BrownEgg]);
        Assert.Equal(new[] { "alpha" }, state.EnabledRoosterSources);
    }

    [Fact]
    public void ApplyDraft_EnableAllProviders_RestoresAllAvailableProviders()
    {
        var state = new CompatSelectionState
        {
            EnabledRoosterSources = new List<string> { "alpha" },
        };

        var selectionService = CreateSelectionService();
        selectionService.InitializeOrRepair(state, out _);
        state.EnabledRoosterSources = new List<string> { "alpha" };

        var service = new RoosterCompatEditorService(selectionService);

        service.ApplyDraft(state, new RoosterCompatEditorDraft
        {
            UseAllProviders = true,
        });

        Assert.Equal(new[] { "alpha", "beta" }, state.EnabledRoosterSources.OrderBy(x => x).ToArray());
    }

    private static CompatSelectionService CreateSelectionService()
    {
        var helper = Substitute.For<IModHelper>();
        var content = Substitute.For<IGameContentHelper>();
        var manifest = Substitute.For<IManifest>();
        var log = Substitute.For<ILog>();

        manifest.UniqueID.Returns("Mikew.RoosterControl.Tests");
        helper.GameContent.Returns(content);

        content.Load<EggSourcesCatalog>("Mods/Mikew.RoosterControl.Tests/compat/egg_sources").Returns(CreateEggCatalog());
        content.Load<RoosterSourcesCatalog>("Mods/Mikew.RoosterControl.Tests/compat/rooster_sources").Returns(CreateRoosterCatalog());

        return new CompatSelectionService(helper, manifest, log);
    }

    private static EggSourcesCatalog CreateEggCatalog()
        => new()
        {
            Sources = new List<EggSourceDef>
            {
                new() { SourceId = "b", DisplayName = "Beta Brown", EggKind = EggKind.BrownEgg, ItemId = "180" },
                new() { SourceId = "a", DisplayName = "Alpha Brown", EggKind = EggKind.BrownEgg, ItemId = "176" },
                new() { SourceId = "lw", DisplayName = "Large White", EggKind = EggKind.LargeWhiteEgg, ItemId = "174" },
                new() { SourceId = "fw", DisplayName = "Fertilized White", EggKind = EggKind.FertilizedWhiteEgg, ItemId = "305" },
                new() { SourceId = "re", DisplayName = "Rooster Egg", EggKind = EggKind.RoosterEgg, ItemId = "999" },
            },
        };

    private static RoosterSourcesCatalog CreateRoosterCatalog()
        => new()
        {
            Sources = new List<RoosterSourceDef>
            {
                new()
                {
                    SourceId = "beta",
                    DisplayName = "Beta Provider",
                    FarmAnimalKeys = new List<RoosterFarmAnimalKeyDef> { new() { Key = "BetaRooster" } },
                },
                new()
                {
                    SourceId = "alpha",
                    DisplayName = "Alpha Provider",
                    FarmAnimalKeys = new List<RoosterFarmAnimalKeyDef> { new() { Key = "AlphaRooster" } },
                },
            },
        };

    private static Dictionary<string, FarmAnimalData> CreateAnimalData()
        => new()
        {
            ["AlphaRooster"] = new FarmAnimalData(),
            ["BetaRooster"] = new FarmAnimalData(),
        };
}
