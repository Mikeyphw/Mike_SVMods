using System;
using System.Collections.Generic;
using LetMeHatch.Core.Breeding;
using LetMeHatch.Core.Breeding.Diagnostics;
using Xunit;

namespace LetMeHatch.Tests;

public sealed class BreedingDumpDtosTests
{
    [Fact]
    public void SerializePlans_UsesStableDtoAndNoDateOnlyConverterNeeded()
    {
        var plan = new BreedingPlan(
            PlanId: "20260227-Chicken-0",
            InGameDate: new DateOnly(2026, 2, 27),
            Seed: 0,
            ScopeAnimalKeys: new[] { "Rooster", "Chicken" },
            Pairs: new List<BreedingPairPlan>
            {
                new(
                    FemaleAnimalKey: "Chicken",
                    MaleAnimalKey: "Rooster",
                    OtherMaleKeys: Array.Empty<string>(),
                    Mode: BreedingMode.Normal,
                    EggToClearItemId: "oldEgg",
                    EggToAssignItemId: "newEgg",
                    FertilizedSucceeded: false,
                    MaleQuantity: 1,
                    BaseFertilizedChance: 0.1,
                    AdjustedFertilizedChance: 0.1,
                    MaleChance: 0.1,
                    BoostByMale: 0.0,
                    ApplyDailyLuckBoost: false,
                    SelectedEggType: "newEgg"
                )
            },
            Mutations: new List<BreedingMutation> { new BreedingMutation.EnforceMaleNeverLays("Rooster") },
            Diagnostics: Array.Empty<string>()
        );

        var json = BreedingDumpJson.SerializePlans(new[] { plan }, indented: false);

        Assert.Contains("\"plan_id\":\"20260227-Chicken-0\"", json, StringComparison.Ordinal);
        Assert.Contains("\"iso_date\":\"2026-02-27\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("InGameDate", json, StringComparison.Ordinal);
    }
}
