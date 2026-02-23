using System.Collections.Generic;
using WildEggsHatching.Application.Spawning;
using WildEggsHatching.Infrastructure.ModData;
using WildEggsHatching.Infrastructure.ModData.Models;
using Xunit;

namespace WildEggsHatching.Tests;

public sealed class SpawnPlannerTests
{
    [Fact]
    public void BuildPlan_FiltersByModAndWorld_AndMarksCertain()
    {
        var log = new FakeLog();
        var mods = new FakeMods(new[] { "Vanilla", "My.Mod" });
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

        var eggId = "Mikew.wildeggshatching_ForestWEgg";
        var areas = new Dictionary<string, List<SpawnAreaRuleJson>>
        {
            [eggId] = new()
            {
                new SpawnAreaRuleJson
                {
                    ModID = "Vanilla",
                    Location = "Forest",
                    MapName = "Forest",
                    EggsToSpawn = 1,
                    UseBaseRandomChance = true,
                    RandomChance = 0.25,
                    ExtraConditions = new ExtraConditionsJson()
                },
                new SpawnAreaRuleJson
                {
                    ModID = "My.Mod",
                    Location = "Forest",
                    MapName = "CustomForest",
                    EggsToSpawn = 1,
                    UseBaseRandomChance = true,
                    RandomChance = 0.25,
                    ExtraConditions = new ExtraConditionsJson()
                },
                new SpawnAreaRuleJson
                {
                    ModID = "My.Mod",
                    Location = "Forest",
                    MapName = "MissingMap",
                    EggsToSpawn = 1,
                    ExtraConditions = new ExtraConditionsJson()
                },
                new SpawnAreaRuleJson
                {
                    ModID = "NotLoaded",
                    Location = "Forest",
                    MapName = "Forest",
                    EggsToSpawn = 1,
                    ExtraConditions = new ExtraConditionsJson()
                },
                new SpawnAreaRuleJson
                {
                    ModID = "My.Mod",
                    Location = "Forest",
                    MapName = "DesertTomb",
                    EggsToSpawn = 1,
                    ExtraConditions = new ExtraConditionsJson()
                }
            }
        };

        var snapshot = new ModDataSnapshot(
            WildAnimals: new Dictionary<string, WildAnimalJson>(),
            SpawnAreas: areas,
            SpawnParameters: parameters,
            LocationPacks: packs);

        var planner = new SpawnPlanner(world, mods, conditions, log);

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
