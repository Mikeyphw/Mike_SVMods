using System;
using System.Collections.Generic;
using System.Linq;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.Integrations.StardewUi.WildAnimals;
using EggsInWild.Infrastructure.ModData.Models;
using Microsoft.Xna.Framework;
using StardewValley.GameData.FarmAnimals;
using Xunit;

namespace EggsInWild.Tests;

public sealed class WildAnimalsEditorViewModelTests
{
    [Fact]
    public void SearchAndFilters_UpdateVisibleGroupsWithoutDiscardingSelection()
    {
        var config = new ModConfig();
        var service = CreateService(config);
        var viewModel = new WildAnimalsEditorViewModel(service, () => config, (cfg, draft, _) => service.ApplyDraft(cfg, draft));

        Assert.Equal(1, viewModel.FilteredAnimalCount);

        viewModel.SearchText = "Void";

        Assert.True(viewModel.ShowEmptyState);

        viewModel.ShowInvalidAnimals = true;

        Assert.Single(viewModel.AnimalGroups);
        Assert.Single(viewModel.AnimalGroups.Single().Animals);
        Assert.Equal("VoidChicken", viewModel.AnimalGroups.Single().Animals.Single().Key);
        viewModel.FilterWarnings = true;

        Assert.Single(viewModel.AnimalGroups.Single().Animals);
        Assert.Equal("Chicken", viewModel.SelectedAnimal?.Key);
    }

    [Fact]
    public void SaveCommand_PersistsChangesOnlyWhenDirtyAndValid()
    {
        var config = new ModConfig();
        var service = CreateService(config);
        var viewModel = new WildAnimalsEditorViewModel(service, () => config, (cfg, draft, _) => service.ApplyDraft(cfg, draft));

        Assert.False(viewModel.SaveCommand.CanExecute(null));

        var selected = Assert.IsType<WildAnimalDraftViewModel>(viewModel.SelectedAnimal);
        selected.PriceText = "nope";
        Assert.False(viewModel.SaveCommand.CanExecute(null));

        selected.PriceText = "240";
        selected.Enabled = false;
        Assert.True(viewModel.SaveCommand.CanExecute(null));

        viewModel.SaveCommand.Execute(null);

        Assert.False(config.WildAnimalOverrides["Chicken"].Enabled);
        Assert.Equal(240, config.WildAnimalOverrides["Chicken"].Price);
        Assert.Equal("Reloaded Chicken.", viewModel.StatusText);
    }

    [Fact]
    public void SelectionChange_WithDirtyDraftRequiresConfirmation()
    {
        var config = new ModConfig();
        var service = CreateService(config);
        var viewModel = new WildAnimalsEditorViewModel(service, () => config, (cfg, draft, _) => service.ApplyDraft(cfg, draft));

        var selected = Assert.IsType<WildAnimalDraftViewModel>(viewModel.SelectedAnimal);
        selected.PriceText = "250";

        viewModel.ShowInvalidAnimals = true;
        var target = viewModel.AnimalGroups.SelectMany(group => group.Animals).Single(item => item.Key == "VoidChicken");
        target.Open.Execute(null);

        Assert.True(viewModel.ShowSelectionConfirmation);
        Assert.Equal("Chicken", viewModel.SelectedAnimal?.Key);

        viewModel.ConfirmSelectionChangeCommand.Execute(null);

        Assert.Equal("VoidChicken", viewModel.SelectedAnimal?.Key);
        Assert.False(viewModel.ShowSelectionConfirmation);
    }

    [Fact]
    public void DiscardCommand_RestoresLoadedState()
    {
        var config = new ModConfig();
        var service = CreateService(config);
        var viewModel = new WildAnimalsEditorViewModel(service, () => config, (cfg, draft, _) => service.ApplyDraft(cfg, draft));

        var selected = Assert.IsType<WildAnimalDraftViewModel>(viewModel.SelectedAnimal);
        selected.PriceText = "999";

        viewModel.DiscardCommand.Execute(null);

        selected = Assert.IsType<WildAnimalDraftViewModel>(viewModel.SelectedAnimal);
        Assert.Equal("120", selected.PriceText);
        Assert.False(selected.IsDirty);
    }

    [Fact]
    public void CanCloseMenu_WithDirtyDraftRequiresSecondConfirmation()
    {
        var config = new ModConfig();
        var service = CreateService(config);
        var viewModel = new WildAnimalsEditorViewModel(service, () => config, (cfg, draft, _) => service.ApplyDraft(cfg, draft));

        var selected = Assert.IsType<WildAnimalDraftViewModel>(viewModel.SelectedAnimal);
        selected.PriceText = "999";

        Assert.False(viewModel.CanCloseMenu());
        Assert.True(viewModel.ShowCloseConfirmation);
        Assert.True(viewModel.CanCloseMenu());
    }

    [Fact]
    public void SelectedAnimal_UsesMergedProgressionSection()
    {
        var config = new ModConfig();
        var service = CreateService(config);
        var viewModel = new WildAnimalsEditorViewModel(service, () => config, (cfg, draft, _) => service.ApplyDraft(cfg, draft));

        var selected = Assert.IsType<WildAnimalDraftViewModel>(viewModel.SelectedAnimal);
        Assert.True(selected.HasProgressionSettings);
        Assert.Equal("Enabled", selected.ProgressionDefaultText);
        Assert.Equal("Default (Enabled)", selected.ProgressionCurrentText);
        Assert.False(selected.ProgressionDirty);

        selected.ForceProgressionDisabled();

        Assert.Equal("Force disabled", selected.ProgressionCurrentText);
        Assert.True(selected.ProgressionDirty);
        Assert.Equal("Disabled", selected.ProgressionEffectiveText);
    }

    [Fact]
    public void StartAddWildAnimal_UsesPickerAndWritesNewDefinition()
    {
        var config = new ModConfig();
        Dictionary<string, WildAnimalJson>? saved = null;
        var service = CreateService(config, value => saved = value);
        var viewModel = new WildAnimalsEditorViewModel(service, () => config, (cfg, draft, _) => service.ApplyDraft(cfg, draft));

        viewModel.StartAddWildAnimal();
        Assert.True(viewModel.IsAuthoringNewAnimal);

        var pickerItem = Assert.Single(viewModel.PickerItems);
        pickerItem.ToggleSelection();
        viewModel.NewWildAnimalKey = "SnowHen";
        viewModel.ConfirmAuthoring();

        Assert.NotNull(saved);
        Assert.True(saved!.ContainsKey("SnowHen"));
        Assert.Equal("SnowHen", viewModel.SelectedAnimal?.Key);
    }

    [Fact]
    public void StartAddModGroup_UsesPickerAndWritesDefinition()
    {
        var config = new ModConfig();
        Dictionary<string, WildAnimalJson>? saved = null;
        var service = CreateService(config, value => saved = value);
        var viewModel = new WildAnimalsEditorViewModel(service, () => config, (cfg, draft, _) => service.ApplyDraft(cfg, draft));

        viewModel.StartAddModGroup();
        Assert.True(viewModel.IsAuthoringNewGroup);

        var pickerItem = Assert.Single(viewModel.PickerItems);
        pickerItem.ToggleSelection();
        viewModel.AuthoringModGroupId = "Custom.Mod";
        viewModel.ConfirmAuthoring();

        Assert.NotNull(saved);
        Assert.True(saved!["Chicken"].ExtraModAnimalKeys.ContainsKey("Custom.Mod"));
    }

    private static WildAnimalsEditorService CreateService(ModConfig config, Action<Dictionary<string, WildAnimalJson>>? saveDefinitions = null)
    {
        return new WildAnimalsEditorService(
            () => new Dictionary<string, WildAnimalJson>(StringComparer.OrdinalIgnoreCase)
            {
                ["Chicken"] = new()
                {
                    Price = 120,
                    LocationTypes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { ["Farm"] = true },
                    ExtraModAnimalKeys = new Dictionary<string, ExtraModAnimalKeysJson>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Vanilla"] = new() { FarmAnimalKeys = new List<string> { "Chicken" } },
                    },
                },
                ["VoidChicken"] = new()
                {
                    Price = 500,
                    IsMagic = true,
                    LocationTypes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { ["Forest"] = false },
                    ExtraModAnimalKeys = new Dictionary<string, ExtraModAnimalKeysJson>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Vanilla"] = new() { FarmAnimalKeys = new List<string> { "MissingAnimal" } },
                    },
                },
            },
            () => config,
            () => new Dictionary<string, FarmAnimalData>(StringComparer.OrdinalIgnoreCase)
            {
                ["Chicken"] = new() { Texture = "Animals/Chicken", SpriteWidth = 16, SpriteHeight = 16 },
            },
            (key, wildAnimal) =>
            {
                foreach (var block in wildAnimal.ExtraModAnimalKeys.Values)
                {
                    foreach (var farmAnimalKey in block.GetEffectiveFarmAnimalKeys())
                    {
                        if (string.Equals(farmAnimalKey, "Chicken", StringComparison.OrdinalIgnoreCase))
                        {
                            return new WildAnimalsEditorPreviewRecord(
                                "Chicken",
                                "Animals/Chicken",
                                new Rectangle(0, 0, 16, 16),
                                true,
                                "Preview: Chicken");
                        }
                    }
                }

                return new WildAnimalsEditorPreviewRecord(null, null, null, false, $"No preview available for {key}.");
            },
            null,
            saveDefinitions);
    }
}
