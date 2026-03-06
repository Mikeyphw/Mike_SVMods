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

public sealed class WildAnimalsEditorServiceTests
{
    [Fact]
    public void LoadCatalog_OrdersAnimalsCollectsLocationTypesAndResolvesPreview()
    {
        var config = new ModConfig();
        var service = CreateService(
            new Dictionary<string, WildAnimalJson>(StringComparer.OrdinalIgnoreCase)
            {
                ["zebra"] = new()
                {
                    LocationTypes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { ["Forest"] = true },
                    ExtraModAnimalKeys = new Dictionary<string, ExtraModAnimalKeysJson>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Vanilla"] = new() { FarmAnimalKeys = new List<string> { "Cow" } },
                    },
                },
                ["alpaca"] = new()
                {
                    LocationTypes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { ["Farm"] = true },
                },
            },
            config,
            new Dictionary<string, FarmAnimalData>(StringComparer.OrdinalIgnoreCase)
            {
                ["Cow"] = new() { Texture = "Animals/Cow", SpriteWidth = 16, SpriteHeight = 16 },
            });

        var catalog = service.LoadCatalog();

        Assert.Equal(new[] { "alpaca", "zebra" }, catalog.Animals.Select(animal => animal.Key));
        Assert.Equal(new[] { "Farm", "Forest" }, catalog.LocationTypeKeys);

        var zebra = catalog.Animals.Last();
        Assert.True(zebra.Preview.HasPreview);
        Assert.Equal("Preview: Cow", zebra.Preview.StatusText);
        Assert.False(zebra.HasWarnings);
    }

    [Fact]
    public void LoadCatalog_AddsWarningsForUnknownFarmAnimalKeys()
    {
        var config = new ModConfig();
        var service = CreateService(
            new Dictionary<string, WildAnimalJson>(StringComparer.OrdinalIgnoreCase)
            {
                ["Rabbit"] = new()
                {
                    ExtraModAnimalKeys = new Dictionary<string, ExtraModAnimalKeysJson>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Author.Mod"] = new() { FarmAnimalKeys = new List<string> { "MissingAnimal" } },
                    },
                },
            },
            config);

        var catalog = service.LoadCatalog();

        var rabbit = Assert.Single(catalog.Animals);
        Assert.True(rabbit.HasWarnings);
        Assert.Contains(rabbit.WarningMessages, message => message.Contains("MissingAnimal", StringComparison.Ordinal));
    }

    [Fact]
    public void DescribeDraft_UsesDraftKeysForPreviewAndWarnings()
    {
        var config = new ModConfig();
        var service = CreateService(
            new Dictionary<string, WildAnimalJson>(StringComparer.OrdinalIgnoreCase)
            {
                ["Chicken"] = new()
                {
                    ExtraModAnimalKeys = new Dictionary<string, ExtraModAnimalKeysJson>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Vanilla"] = new() { FarmAnimalKeys = new List<string> { "Cow" } },
                    },
                },
            },
            config,
            new Dictionary<string, FarmAnimalData>(StringComparer.OrdinalIgnoreCase)
            {
                ["Duck"] = new() { Texture = "Animals/Duck", SpriteWidth = 32, SpriteHeight = 16 },
            });

        var draft = new WildAnimalDraft(
            "Chicken",
            BaseEnabled: true,
            Enabled: true,
            BaseLargeEgg: false,
            LargeEgg: false,
            BasePrice: 100,
            Price: 100,
            BaseIsMagic: false,
            IsMagic: false,
            LocationTypes: Array.Empty<WildAnimalDraftLocationType>(),
            ModGroups: new[]
            {
                new WildAnimalDraftModGroup("Vanilla", new[] { "Cow" }, new[] { "Duck", "Missing" }),
            });

        var visual = service.DescribeDraft(draft);

        Assert.True(visual.Preview.HasPreview);
        Assert.Equal("Preview: Duck", visual.Preview.StatusText);
        Assert.True(visual.HasWarnings);
        Assert.Contains("Missing", visual.WarningMessages.Single());
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
        ModConfig config,
        Dictionary<string, FarmAnimalData>? farmAnimals = null)
    {
        farmAnimals ??= new Dictionary<string, FarmAnimalData>(StringComparer.OrdinalIgnoreCase);
        return new WildAnimalsEditorService(
            () => definitions.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
            () => config,
            () => farmAnimals,
            (key, wildAnimal) =>
            {
                foreach (var block in wildAnimal.ExtraModAnimalKeys.Values)
                {
                    foreach (var animalKey in block.GetEffectiveFarmAnimalKeys())
                    {
                        if (farmAnimals.TryGetValue(animalKey, out var data))
                        {
                            return new WildAnimalsEditorPreviewRecord(
                                animalKey,
                                data.Texture,
                                new Rectangle(0, 0, data.SpriteWidth, data.SpriteHeight),
                                true,
                                $"Preview: {animalKey}");
                        }
                    }
                }

                return new WildAnimalsEditorPreviewRecord(null, null, null, false, $"No preview available for {key}.");
            });
    }
}
