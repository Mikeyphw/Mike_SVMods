using System;
using System.Collections.Generic;
using LetMeHatch.Core.Breeding;
using StardewValley.GameData.FarmAnimals;
using Xunit;

namespace LetMeHatch.Tests;

public sealed class BreedingPlanExecutorManagedEggTrackingTests
{
    [Fact]
    public void Apply_ClearsOnlyLmhManagedEggsAndUpdatesTracking()
    {
        var wildEggs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "WildEgg" };
        var executor = new BreedingPlanExecutor(wildEggs);

        var female = new FarmAnimalData
        {
            EggItemIds = new List<string> { "WildEgg", "LmhEgg1", "OtherModEgg" },
            CustomFields = new Dictionary<string, string>
            {
                ["LetMeHatch.ManagedEggItemIds"] = "LmhEgg1|LmhEgg2"
            }
        };

        var male = new FarmAnimalData { Gender = FarmAnimalGender.Male };

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
            EggToClearItemId: "LmhEgg1",
            EggToAssignItemId: "LmhEgg3",
            FertilizedSucceeded: false,
            MaleQuantity: 1,
            BaseFertilizedChance: 0.10,
            AdjustedFertilizedChance: 0.10,
            MaleChance: 0.10,
            BoostByMale: 0.0,
            ApplyDailyLuckBoost: false,
            SelectedEggType: "LmhEgg3"
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
        Assert.Contains("OtherModEgg", female.EggItemIds!, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("LmhEgg3", female.EggItemIds!, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("LmhEgg1", female.EggItemIds!, StringComparer.OrdinalIgnoreCase);

        Assert.True(female.CustomFields.TryGetValue("LetMeHatch.ManagedEggItemIds", out var managed));
        Assert.Contains("LmhEgg3", managed, StringComparison.Ordinal);
        Assert.DoesNotContain("LmhEgg1", managed, StringComparison.Ordinal);
    }
}
