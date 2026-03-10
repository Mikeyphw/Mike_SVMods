using System.Collections.Generic;
using System.Linq;
using EggsInWild.Core.Enums;
using EggsInWild.Infrastructure.ModData;
using EggsInWild.Infrastructure.ModData.Models;
using StardewValley.GameData.FarmAnimals;
using Xunit;

namespace EggsInWild.Tests;

public sealed class WildEggSpawnCatalogBuilderTests
{
    [Fact]
    public void Build_DeclaresAllFourVariants_PerLocation_EvenWhenNullOrEmpty()
    {
        var definitions = new Dictionary<string, WildEggSpawnLocationJson>
        {
            ["Forest"] = new()
            {
                Normal = new WildEggSpawnSizeJson
                {
                    Common = new List<SpawnAreaRuleJson>(),
                    Magic = null,
                },
                Large = new WildEggSpawnSizeJson
                {
                    Common = new List<SpawnAreaRuleJson>(),
                    Magic = new List<SpawnAreaRuleJson>(),
                }
            }
        };

        var wildAnimals = new Dictionary<string, WildAnimalJson>(StringComparer.OrdinalIgnoreCase);
        var farmAnimals = new Dictionary<string, FarmAnimalData>(StringComparer.OrdinalIgnoreCase);
        var catalog = WildEggSpawnCatalogBuilder.Build(definitions, wildAnimals, farmAnimals, new FakeModRegistry(Array.Empty<string>()), new FakeLog());

        Assert.Equal(4, catalog.DeclaredEggDefinitions.Count);
        Assert.Contains(catalog.DeclaredEggDefinitions, x => x.EggItemId == "Mikew.eggsinwild_ForestWEgg");
        Assert.Contains(catalog.DeclaredEggDefinitions, x => x.EggItemId == "Mikew.eggsinwild_ForestLWEgg");
        Assert.Contains(catalog.DeclaredEggDefinitions, x => x.EggItemId == "Mikew.eggsinwild_ForestMWEgg");
        Assert.Contains(catalog.DeclaredEggDefinitions, x => x.EggItemId == "Mikew.eggsinwild_ForestLMWEgg");
    }

    [Fact]
    public void Build_ActivatesOnlyVariantsWithValidCandidates()
    {
        var definitions = new Dictionary<string, WildEggSpawnLocationJson>
        {
            ["Forest"] = new()
            {
                Normal = new WildEggSpawnSizeJson
                {
                    Common = new List<SpawnAreaRuleJson> { new() { MapName = "Forest", ModID = "Vanilla" } },
                    Magic = new List<SpawnAreaRuleJson> { new() { MapName = "Forest", ModID = "Vanilla" } },
                },
                Large = new WildEggSpawnSizeJson(),
            }
        };

        var wildAnimals = new Dictionary<string, WildAnimalJson>(StringComparer.OrdinalIgnoreCase)
        {
            ["White Chicken"] = new()
            {
                Enabled = true,
                LargeEgg = false,
                IsMagic = false,
                LocationTypes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { ["Forest"] = true },
                ExtraModAnimalKeys = new Dictionary<string, ExtraModAnimalKeysJson>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Vanilla"] = new ExtraModAnimalKeysJson { FarmAnimalKeys = new() { "White Chicken" } }
                }
            },
            ["Mystery Chicken"] = new()
            {
                Enabled = true,
                LargeEgg = false,
                IsMagic = true,
                LocationTypes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { ["Forest"] = true },
                ExtraModAnimalKeys = new Dictionary<string, ExtraModAnimalKeysJson>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Missing.Mod"] = new ExtraModAnimalKeysJson { FarmAnimalKeys = new() { "Mystery Chicken" } }
                }
            }
        };

        var farmAnimals = new Dictionary<string, FarmAnimalData>(StringComparer.OrdinalIgnoreCase)
        {
            ["White Chicken"] = new() { House = "Coop" },
            ["Mystery Chicken"] = new() { House = "Coop" },
        };

        var catalog = WildEggSpawnCatalogBuilder.Build(definitions, wildAnimals, farmAnimals, new FakeModRegistry(Array.Empty<string>()), new FakeLog());

        Assert.Contains(catalog.ActiveEggDefinitions, x => x.Kind == EggKind.Wild);
        Assert.DoesNotContain(catalog.ActiveEggDefinitions, x => x.Kind == EggKind.Magic);
    }
}
