using System;
using System.Collections.Generic;
using System.Linq;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.Integrations.StardewUi.WildAnimals;
using EggsInWild.Infrastructure.ModData.Models;
using Xunit;

namespace EggsInWild.Tests;

public sealed class WildAnimalsEditorServiceTests
{
    [Fact]
    public void LoadCatalog_OrdersAnimalsAndCollectsLocationTypes()
    {
        var config = new ModConfig();
        var service = CreateService(
            new Dictionary<string, WildAnimalJson>(StringComparer.OrdinalIgnoreCase)
            {
                ["zebra"] = new() { LocationTypes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { ["Forest"] = true } },
                ["alpaca"] = new() { LocationTypes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { ["Farm"] = true } },
            },
            config);

        var catalog = service.LoadCatalog();

        Assert.Equal(new[] { "alpaca", "zebra" }, catalog.Animals.Select(animal => animal.Key));
        Assert.Equal(new[] { "Farm", "Forest" }, catalog.LocationTypeKeys);
    }

    [Fact]
    public void ApplyDraft_ScalarChangesPersistAsOverrides()
    {
        var config = new ModConfig();
        var service = CreateService(
            new Dictionary<string, WildAnimalJson>(StringComparer.OrdinalIgnoreCase)
            {
                ["Chicken"] = new()
                {
                    Enabled = true,
                    LargeEgg = false,
                    Price = 100,
                    IsMagic = false,
                },
            },
            config);

        var draft = service.CreateDraft("Chicken") with
        {
            Enabled = false,
            LargeEgg = true,
            Price = 250,
            IsMagic = true,
        };

        service.ApplyDraft(config, draft);

        var ov = Assert.Single(config.WildAnimalOverrides);
        Assert.Equal("Chicken", ov.Key);
        Assert.False(ov.Value.Enabled);
        Assert.True(ov.Value.LargeEgg);
        Assert.Equal(250, ov.Value.Price);
        Assert.True(ov.Value.IsMagic);
    }

    [Fact]
    public void ApplyDraft_LocationTypeDifferencesPersistAndMatchingValuesAreRemoved()
    {
        var config = new ModConfig();
        var service = CreateService(
            new Dictionary<string, WildAnimalJson>(StringComparer.OrdinalIgnoreCase)
            {
                ["Duck"] = new()
                {
                    LocationTypes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Farm"] = true,
                        ["Forest"] = false,
                    },
                },
            },
            config);

        var draft = service.CreateDraft("Duck") with
        {
            LocationTypes = new[]
            {
                new WildAnimalDraftLocationType("Farm", true, true),
                new WildAnimalDraftLocationType("Forest", false, true),
            },
        };

        service.ApplyDraft(config, draft);

        var ov = Assert.Single(config.WildAnimalOverrides).Value;
        Assert.NotNull(ov.LocationTypes);
        Assert.Single(ov.LocationTypes!);
        Assert.True(ov.LocationTypes["Forest"]);
        Assert.False(ov.LocationTypes.ContainsKey("Farm"));
    }

    [Fact]
    public void ApplyDraft_ExtraModAnimalKeysMatchingBaseRemoveOverride()
    {
        var config = new ModConfig
        {
            WildAnimalOverrides = new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase)
            {
                ["Rabbit"] = new()
                {
                    ExtraModAnimalKeys = new Dictionary<string, ExtraModAnimalKeysOverrideConfig>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Author.Mod"] = new() { FarmAnimalKeys = new List<string> { "Goat" } },
                    },
                },
            },
        };

        var service = CreateService(
            new Dictionary<string, WildAnimalJson>(StringComparer.OrdinalIgnoreCase)
            {
                ["Rabbit"] = new()
                {
                    ExtraModAnimalKeys = new Dictionary<string, ExtraModAnimalKeysJson>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Author.Mod"] = new() { FarmAnimalKeys = new List<string> { "Cow" } },
                    },
                },
            },
            config);

        var draft = service.CreateDraft("Rabbit") with
        {
            ModGroups = new[]
            {
                new WildAnimalDraftModGroup("Author.Mod", new[] { "Cow" }, new[] { "Cow" }),
            },
        };

        service.ApplyDraft(config, draft);

        Assert.Empty(config.WildAnimalOverrides);
    }

    private static WildAnimalsEditorService CreateService(
        Dictionary<string, WildAnimalJson> definitions,
        ModConfig config)
    {
        return new WildAnimalsEditorService(
            () => definitions.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
            () => config);
    }
}
