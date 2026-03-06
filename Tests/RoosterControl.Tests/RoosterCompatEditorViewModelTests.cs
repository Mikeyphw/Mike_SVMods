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

public sealed class RoosterCompatEditorViewModelTests
{
    [Fact]
    public void SelectProvider_UpdatesSelectedProviderAndSaveFlags()
    {
        var saved = 0;
        var state = new CompatSelectionState();
        var service = CreateEditorService(state);
        var viewModel = new RoosterCompatEditorViewModel(service, state, CreateAnimalData, () => saved++);

        var provider = viewModel.ProviderOptions.Single(p => p.ProviderId == "alpha");
        viewModel.SelectProvider(provider);

        Assert.Equal("Alpha Provider", viewModel.SelectedProviderName);
        Assert.True(viewModel.CanSave);
        Assert.Equal(0, saved);
    }

    [Fact]
    public void Save_AppliesDraftAndInvokesPersistCallback()
    {
        var saved = 0;
        var state = new CompatSelectionState();
        var service = CreateEditorService(state);
        var viewModel = new RoosterCompatEditorViewModel(service, state, CreateAnimalData, () => saved++);

        viewModel.SetEggSource(EggKind.BrownEgg, "a");
        var provider = viewModel.ProviderOptions.Single(p => p.ProviderId == "alpha");
        viewModel.SelectProvider(provider);

        viewModel.Save();

        Assert.Equal(1, saved);
        Assert.Equal("a", state.SelectedEggSources[EggKind.BrownEgg]);
        Assert.Equal(new[] { "alpha" }, state.EnabledRoosterSources);
        Assert.False(viewModel.CanSave);
    }

    [Fact]
    public void ResetSelection_ArmsUseAllProvidersAndDiscardRestoresSnapshot()
    {
        var state = new CompatSelectionState();
        var service = CreateEditorService(state);
        var viewModel = new RoosterCompatEditorViewModel(service, state, CreateAnimalData, () => { });

        var provider = viewModel.ProviderOptions.Single(p => p.ProviderId == "alpha");
        viewModel.SelectProvider(provider);
        viewModel.ResetSelection();

        Assert.True(viewModel.CanSave);
        Assert.Equal("All providers", viewModel.SelectedProviderName);

        viewModel.Discard();

        Assert.False(viewModel.CanSave);
        Assert.Equal("All providers", viewModel.SelectedProviderName);
    }

    [Fact]
    public void ValidationText_IncludesRoosterValidationLine()
    {
        var state = new CompatSelectionState();
        var service = CreateEditorService(state);
        var viewModel = new RoosterCompatEditorViewModel(service, state, CreateAnimalData, () => { });

        Assert.True(viewModel.HasValidation);
        Assert.Contains("Roosters:", viewModel.ValidationText);
    }

    private static RoosterCompatEditorService CreateEditorService(CompatSelectionState state)
    {
        var helper = Substitute.For<IModHelper>();
        var content = Substitute.For<IGameContentHelper>();
        var manifest = Substitute.For<IManifest>();
        var log = Substitute.For<ILog>();

        manifest.UniqueID.Returns("Mikew.RoosterControl.Tests");
        helper.GameContent.Returns(content);

        content.Load<EggSourcesCatalog>("Mods/Mikew.RoosterControl.Tests/compat/egg_sources").Returns(
            new EggSourcesCatalog
            {
                Sources = new List<EggSourceDef>
                {
                    new() { SourceId = "b", DisplayName = "Beta Brown", EggKind = EggKind.BrownEgg, ItemId = "180" },
                    new() { SourceId = "a", DisplayName = "Alpha Brown", EggKind = EggKind.BrownEgg, ItemId = "176" },
                    new() { SourceId = "lw", DisplayName = "Large White", EggKind = EggKind.LargeWhiteEgg, ItemId = "174" },
                    new() { SourceId = "fw", DisplayName = "Fertilized White", EggKind = EggKind.FertilizedWhiteEgg, ItemId = "305" },
                    new() { SourceId = "re", DisplayName = "Rooster Egg", EggKind = EggKind.RoosterEgg, ItemId = "999" },
                },
            });

        content.Load<RoosterSourcesCatalog>("Mods/Mikew.RoosterControl.Tests/compat/rooster_sources").Returns(
            new RoosterSourcesCatalog
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
            });

        var selectionService = new CompatSelectionService(helper, manifest, log);
        selectionService.InitializeOrRepair(state, out _);
        return new RoosterCompatEditorService(selectionService);
    }

    private static IDictionary<string, FarmAnimalData> CreateAnimalData()
        => new Dictionary<string, FarmAnimalData>
        {
            ["AlphaRooster"] = new(),
            ["BetaRooster"] = new(),
        };
}
