using System;
using System.Collections.Generic;
using Mikew.NestCore.Models;
using RoosterControl.Application.Planning;
using RoosterControl.Enums;
using RoosterControl.Framework.Config;
using Xunit;

namespace RoosterControl.Tests;

public sealed class DailyHatchingPlanServiceTests
{
    [Fact]
    public void ComputeFingerprint_IsStable_ForSameInputs()
    {
        var config = RoosterControlConfig.CreateDefault();

        var rooster = new RoosterData
        {
            RoosterType = RoosterType.White,
            FarmAnimalName = "Chicken",
            Female = "Chicken",
            BuildingRequired = RequiredBuilding.BigCoop,
            Quantity = 1,
            Eggs = new Dictionary<EggTypeEntries, Dictionary<RoosterItemDataEntries, string>>
            {
                [EggTypeEntries.Normal] = new()
                {
                    [RoosterItemDataEntries.Name] = "176",
                    [RoosterItemDataEntries.Probability] = "50"
                }
            },
            Females = new List<string> { "Chicken" }
        };

        var roosters = new Dictionary<RoosterType, RoosterData> { [RoosterType.White] = rooster };
        var femaleEggs = new Dictionary<string, Dictionary<EggTypeEntries, string>>(StringComparer.OrdinalIgnoreCase);

        var svc = new DailyHatchingPlanService();

        var fp1 = svc.ComputeFingerprint(config, roosters, femaleEggs, dailyLuck: 0, farmId: 123, day: 5);
        var fp2 = svc.ComputeFingerprint(config, roosters, femaleEggs, dailyLuck: 0, farmId: 123, day: 5);

        Assert.Equal(fp1, fp2);
    }

    [Fact]
    public void BuildPlan_RollsAlwaysTrue_For100Percent()
    {
        var config = RoosterControlConfig.CreateDefault();
        config.General.EnableEggProbability = true;

        var rooster = new RoosterData
        {
            RoosterType = RoosterType.White,
            FarmAnimalName = "Chicken",
            Female = "Chicken",
            BuildingRequired = RequiredBuilding.BigCoop,
            Quantity = 1,
            Eggs = new Dictionary<EggTypeEntries, Dictionary<RoosterItemDataEntries, string>>
            {
                [EggTypeEntries.Normal] = new()
                {
                    [RoosterItemDataEntries.Name] = "176",
                    [RoosterItemDataEntries.Probability] = "100"
                }
            },
            Females = new List<string> { "Chicken" }
        };

        var roosters = new Dictionary<RoosterType, RoosterData> { [RoosterType.White] = rooster };
        var femaleEggs = new Dictionary<string, Dictionary<EggTypeEntries, string>>(StringComparer.OrdinalIgnoreCase);

        var svc = new DailyHatchingPlanService();

        var envelope = svc.BuildPlan(
            farmAnimalsData: new Dictionary<string, StardewValley.GameData.FarmAnimals.FarmAnimalData>(),
            config: config,
            roosters: roosters,
            femaleEggs: femaleEggs,
            dailyLuck: 0,
            farmId: 123,
            day: 5,
            nonce: 1
        );

        var plan = envelope.Buildings["Big Coop"].Roosters["Chicken"].Females["Chicken"].NormalEgg!;
        Assert.True(plan.RolledSuccess);
        Assert.Equal(100, plan.ChancePercent);
    }
}
