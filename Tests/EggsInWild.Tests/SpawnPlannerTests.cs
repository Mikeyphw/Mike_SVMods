using System.Collections.Generic;
using EggsInWild.Application.Spawning;
using EggsInWild.Infrastructure.ModData;
using EggsInWild.Infrastructure.ModData.Models;
using Xunit;

namespace EggsInWild.Tests;

public sealed class SpawnPlannerTests
{
    [Fact]
    public void BuildPlan_FiltersByModAndWorld_AndMarksCertain()
    {
        var log = new FakeLog();
        var mods = new FakeMods(new[] { "Vanilla", "My.Mod" });
        var registry = new FakeModRegistry(new[] { "My.Mod" });
        var world = new FakeWorld(new[] { "Forest", "CustomForest", "DesertTomb" });
        var conditions = new AlwaysTrueConditions();

        var parameters = new SpawnParametersJson
        {
            BaseRandomChance = 0.2,
            Parameters_Vanilla = new ParameterPackJson { CertainEggs = 1, UseJsonValues = true, MaxEggPack = 2, MinEggArea = 1, MaxEggArea = 1 },
            Parameters_Packs = new ParameterPackJson { CertainEggs = 1, UseJsonValues = true, MaxEggPack = 2, MinEggArea = 1, MaxEggArea = 1 },
        };

        var packs = new Dictionary<string, LocationPackJson>
        {
            ["Vanilla"] = new LocationPackJson
            {
                MinimumSpawnsPerDay = 1,
                MaximumSpawnsPerDay = 1,
                Locations = new Dictionary<string, List<string>>
                {
                    ["Forest"] = new List<string> { "Forest" }
                }
            },
            ["My.Mod"] = new LocationPackJson
            {
                MinimumSpawnsPerDay = 1,
                MaximumSpawnsPerDay = 1,
                Locations = new Dictionary<string, List<string>>
                {
                    ["Forest"] = new List<string> { "CustomForest" }
                }
            },
            ["NotLoaded"] = new LocationPackJson
            {
                MinimumSpawnsPerDay = 1,
                MaximumSpawnsPerDay = 1,
                Locations = new Dictionary<string, List<string>>
                {
                    ["Forest"] = new List<string> { "Forest" }
                }
            }
        };

        var areas = new Dictionary<string, WildEggSpawnLocationJson>
        {
            ["Forest"] = new()
            {
                Normal = new WildEggSpawnSizeJson
                {
                    Common = new()
                    {
                        new SpawnAreaRuleJson
                        {
                            ModID = "Vanilla",
                            MapName = "Forest",
                            EggsToSpawn = 1,
                            UseBaseRandomChance = true,
                            RandomChance = 0.25,
                            ExtraConditions = new ExtraConditionsJson()
                        },
                        new SpawnAreaRuleJson
                        {
                            ModID = "My.Mod",
                            MapName = "CustomForest",
                            EggsToSpawn = 1,
                            UseBaseRandomChance = true,
                            RandomChance = 0.25,
                            ExtraConditions = new ExtraConditionsJson()
                        },
                        new SpawnAreaRuleJson
                        {
                            ModID = "My.Mod",
                            MapName = "MissingMap",
                            EggsToSpawn = 1,
                            ExtraConditions = new ExtraConditionsJson()
                        },
                        new SpawnAreaRuleJson
                        {
                            ModID = "NotLoaded",
                            MapName = "Forest",
                            EggsToSpawn = 1,
                            ExtraConditions = new ExtraConditionsJson()
                        },
                        new SpawnAreaRuleJson
                        {
                            ModID = "My.Mod",
                            MapName = "DesertTomb",
                            EggsToSpawn = 1,
                            ExtraConditions = new ExtraConditionsJson()
                        }
                    }
                }
            }
        };

        var snapshot = new ModDataSnapshot(
            WildAnimals: new Dictionary<string, WildAnimalJson>(StringComparer.OrdinalIgnoreCase)
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
                }
            },
            WildEggSpawnDefinitions: areas,
            SpawnParameters: parameters,
            LocationPacks: packs);

        var farmAnimals = new Dictionary<string, StardewValley.GameData.FarmAnimals.FarmAnimalData>(StringComparer.OrdinalIgnoreCase)
        {
            ["White Chicken"] = new() { House = "Coop" }
        };

        var planner = new SpawnPlanner(world, mods, registry, conditions, log, () => farmAnimals);

        var plan = planner.BuildPlan(snapshot, saveId: 123, dayNumber: 456);

        Assert.Equal(2, plan.Jobs.Count);
        Assert.Contains(plan.Jobs, j => j.MapName == "Forest");
        Assert.Contains(plan.Jobs, j => j.MapName == "CustomForest");

        Assert.Contains(plan.Jobs, j => j.PackId == "Vanilla" && j.BypassRandomChance);
        Assert.Contains(plan.Jobs, j => j.PackId == "My.Mod" && j.BypassRandomChance);

        Assert.Equal(2, plan.PackEggBudgets.Count);
        Assert.Equal(1, plan.PackEggBudgets["Vanilla"]);
        Assert.Equal(1, plan.PackEggBudgets["My.Mod"]);
    }
}
