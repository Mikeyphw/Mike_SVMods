using System;
using System.Collections.Generic;
using System.Linq;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.Integrations.StardewUi.WildAnimals;
using EggsInWild.Infrastructure.ModData.Models;
using Xunit;

namespace EggsInWild.Tests;

public sealed class WildAnimalsEditorViewModelTests
{
    [Fact]
    public void SaveCommand_PersistsSelectedAnimalChanges()
    {
        var config = new ModConfig();
        var service = CreateService(config);
        var savedDrafts = new List<WildAnimalDraft>();
        var viewModel = new WildAnimalsEditorViewModel(
            service,
            () => config,
            (cfg, draft) =>
            {
                savedDrafts.Add(draft);
                service.ApplyDraft(cfg, draft);
            });

        var selected = Assert.IsType<WildAnimalDraftViewModel>(viewModel.SelectedAnimal);
        selected.Enabled = false;
        selected.PriceText = "240";

        viewModel.SaveCommand.Execute(null);

        Assert.Single(savedDrafts);
        Assert.Equal("Chicken", savedDrafts[0].Key);
        Assert.False(config.WildAnimalOverrides["Chicken"].Enabled);
        Assert.Equal(240, config.WildAnimalOverrides["Chicken"].Price);
    }

    [Fact]
    public void DiscardCommand_RestoresLoadedState()
    {
        var config = new ModConfig();
        var service = CreateService(config);
        var viewModel = new WildAnimalsEditorViewModel(service, () => config, service.ApplyDraft);

        var selected = Assert.IsType<WildAnimalDraftViewModel>(viewModel.SelectedAnimal);
        selected.PriceText = "999";

        viewModel.DiscardCommand.Execute(null);

        selected = Assert.IsType<WildAnimalDraftViewModel>(viewModel.SelectedAnimal);
        Assert.Equal("120", selected.PriceText);
    }

    [Fact]
    public void ReloadCommand_PreservesSelectedAnimalKey()
    {
        var config = new ModConfig();
        var service = CreateService(config);
        var viewModel = new WildAnimalsEditorViewModel(service, () => config, service.ApplyDraft);

        viewModel.AnimalGroups.Last().Animals.Single().Open.Execute(null);
        var selected = Assert.IsType<WildAnimalDraftViewModel>(viewModel.SelectedAnimal);
        Assert.Equal("VoidChicken", selected.Key);

        viewModel.ReloadCommand.Execute(null);

        selected = Assert.IsType<WildAnimalDraftViewModel>(viewModel.SelectedAnimal);
        Assert.Equal("VoidChicken", selected.Key);
    }

    private static WildAnimalsEditorService CreateService(ModConfig config)
    {
        return new WildAnimalsEditorService(
            () => new Dictionary<string, WildAnimalJson>(StringComparer.OrdinalIgnoreCase)
            {
                ["Chicken"] = new()
                {
                    Price = 120,
                    LocationTypes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { ["Farm"] = true },
                },
                ["VoidChicken"] = new()
                {
                    Price = 500,
                    LocationTypes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { ["Forest"] = false },
                },
            },
            () => config);
    }
}
