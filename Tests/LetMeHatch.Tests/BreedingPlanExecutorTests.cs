using System;
using System.Collections.Generic;
using LetMeHatch.Core.Breeding;
using LetMeHatch.Infrastructure.Breeding;
using StardewValley.GameData.FarmAnimals;
using Xunit;

namespace LetMeHatch.Tests;

public sealed class BreedingPlanExecutorTests
{
    [Fact]
    public void Apply_StripsEggLikeProduces_PreservesNonEggProduces_AndEnforcesMaleNeverLays()
    {
        var wildEggs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "WildEgg" };
        var executor = new BreedingPlanExecutor(wildEggs);

        var female = new FarmAnimalData
        {
            EggItemIds = new List<string> { "fooeggbar", "WildEgg" },
            ProduceItemIds = new List<FarmAnimalProduce>
            {
                new() { ItemId = "fooeggbar", Id = "fooeggbar" },
                new() { ItemId = "Wool", Id = "Wool" }
            },
            DeluxeProduceItemIds = new List<FarmAnimalProduce>
            {
                new() { ItemId = "fooeggbar", Id = "fooeggbar" },
                new() { ItemId = "Milk", Id = "Milk" }
            },
            CustomFields = new Dictionary<string, string>
            {
                ["LetMeHatch.ManagedEggItemIds"] = "fooeggbar"
            }
        };

        var male = new FarmAnimalData
        {
            Gender = FarmAnimalGender.Male,
            EggItemIds = new List<string> { "should_be_cleared" }
        };

        var farmAnimals = new Dictionary<string, FarmAnimalData>(StringComparer.OrdinalIgnoreCase)
        {
            ["Chicken"] = female,
            ["Rooster"] = male
        };

        var pair = new BreedingPairPlan(
            FemaleAnimalKey: "Chicken",
            MaleAnimalKey: "Rooster",
            OtherMaleKeys: Array.Empty<string>(),
            Mode: BreedingMode.Normal,
            EggToClearItemId: "fooeggbar",
            EggToAssignItemId: "newEgg",
            FertilizedSucceeded: false,
            MaleQuantity: 1,
            BaseFertilizedChance: 0.10,
            AdjustedFertilizedChance: 0.10,
            MaleChance: 0.10,
            BoostByMale: 0.0,
            ApplyDailyLuckBoost: false,
            SelectedEggType: "newEgg"
        );

        var plan = new BreedingPlan(
            PlanId: "test",
            InGameDate: new DateOnly(2026, 2, 27),
            Seed: 0,
            ScopeAnimalKeys: new[] { "Chicken", "Rooster" },
            Pairs: new List<BreedingPairPlan> { pair },
            Mutations: new List<BreedingMutation> { new BreedingMutation.EnforceMaleNeverLays("Rooster") },
            Diagnostics: Array.Empty<string>()
        );

        executor.Apply(farmAnimals, plan);

        Assert.NotNull(female.EggItemIds);
        Assert.Contains("WildEgg", female.EggItemIds!, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("newEgg", female.EggItemIds!, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("fooeggbar", female.EggItemIds!, StringComparer.OrdinalIgnoreCase);

        Assert.NotNull(female.ProduceItemIds);
        Assert.Contains(female.ProduceItemIds!, p => string.Equals(p.ItemId, "Wool", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(female.ProduceItemIds!, p => string.Equals(p.ItemId, "fooeggbar", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(female.DeluxeProduceItemIds);
        Assert.Contains(female.DeluxeProduceItemIds!, p => string.Equals(p.ItemId, "Milk", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(female.DeluxeProduceItemIds!, p => string.Equals(p.ItemId, "fooeggbar", StringComparison.OrdinalIgnoreCase));

        Assert.Null(male.EggItemIds);
    }
}
