using System;
using System.Collections.Generic;
using LetMeHatch.Core.Breeding;
using LetMeHatch.Infrastructure.Breeding;
using StardewValley.GameData.FarmAnimals;
using Xunit;

namespace LetMeHatch.Tests;

public sealed class BreedingPlanExecutorFertilizedTests
{
    [Fact]
    public void Apply_WhenFertilizedSucceeded_AssignsEggAndUpsertsProduces_PreservingNonEgg()
    {
        var wildEggs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "WildEgg" };
        var executor = new BreedingPlanExecutor(wildEggs);

        var female = new FarmAnimalData
        {
            EggItemIds = new List<string> { "oldEgg", "WildEgg" },
            ProduceItemIds = new List<FarmAnimalProduce>
            {
                new() { ItemId = "oldEgg", Id = "oldEgg" },
                new() { ItemId = "Milk", Id = "Milk" }
            },
            DeluxeProduceItemIds = new List<FarmAnimalProduce>
            {
                new() { ItemId = "oldEgg", Id = "oldEgg" },
                new() { ItemId = "Wool", Id = "Wool" }
            },
            CustomFields = new Dictionary<string, string>
            {
                ["LetMeHatch.ManagedEggItemIds"] = "oldEgg"
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
            Mode: BreedingMode.Fertilized,
            EggToClearItemId: "oldEgg",
            EggToAssignItemId: "FertilizedEgg",
            FertilizedSucceeded: true,
            MaleQuantity: 1,
            BaseFertilizedChance: 0.10,
            AdjustedFertilizedChance: 0.10,
            MaleChance: 0.10,
            BoostByMale: 0.0,
            ApplyDailyLuckBoost: false,
            SelectedEggType: "FertilizedEgg"
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
        Assert.Contains("FertilizedEgg", female.EggItemIds!, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("oldEgg", female.EggItemIds!, StringComparer.OrdinalIgnoreCase);

        Assert.NotNull(female.ProduceItemIds);
        Assert.Contains(female.ProduceItemIds!, p => string.Equals(p.ItemId, "Milk", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(female.ProduceItemIds!, p => string.Equals(p.ItemId, "FertilizedEgg", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(female.ProduceItemIds!, p => string.Equals(p.ItemId, "oldEgg", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(female.DeluxeProduceItemIds);
        Assert.Contains(female.DeluxeProduceItemIds!, p => string.Equals(p.ItemId, "Wool", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(female.DeluxeProduceItemIds!, p => string.Equals(p.ItemId, "FertilizedEgg", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(female.DeluxeProduceItemIds!, p => string.Equals(p.ItemId, "oldEgg", StringComparison.OrdinalIgnoreCase));

        Assert.Null(male.EggItemIds);
    }
}
