using System;
using System.Collections.Generic;
using LetMeHatch.Core.Breeding;
using Xunit;

namespace LetMeHatch.Tests;

public sealed class BreedingPlanValidatorTests
{
    [Fact]
    public void Validate_WhenEnforcementPresentAndOrdered_IsValid()
    {
        var plan = CreatePlan(
            mutations: new List<BreedingMutation>
            {
                new BreedingMutation.StripEggLikeFromProduces("Chicken"),
                new BreedingMutation.EnforceMaleNeverLays("Rooster")
            });

        var validator = new BreedingPlanValidator();
        var result = validator.Validate(plan);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WhenMissingEnforcement_IsInvalid()
    {
        var plan = CreatePlan(mutations: new List<BreedingMutation>());

        var validator = new BreedingPlanValidator();
        var result = validator.Validate(plan);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Missing EnforceMaleNeverLays", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WhenEnforcementNotLast_IsInvalid()
    {
        var plan = CreatePlan(
            mutations: new List<BreedingMutation>
            {
                new BreedingMutation.EnforceMaleNeverLays("Rooster"),
                new BreedingMutation.StripEggLikeFromProduces("Chicken")
            });

        var validator = new BreedingPlanValidator();
        var result = validator.Validate(plan);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("must be contiguous", StringComparison.OrdinalIgnoreCase));
    }

    private static BreedingPlan CreatePlan(IReadOnlyList<BreedingMutation> mutations)
    {
        var pair = new BreedingPairPlan(
            FemaleAnimalKey: "Chicken",
            MaleAnimalKey: "Rooster",
            OtherMaleKeys: Array.Empty<string>(),
            Mode: BreedingMode.Normal,
            EggToClearItemId: "fooeggbar",
            EggToAssignItemId: "baregg",
            FertilizedSucceeded: false,
            MaleQuantity: 1,
            BaseFertilizedChance: 0.10,
            AdjustedFertilizedChance: 0.10,
            MaleChance: 0.10,
            BoostByMale: 0.0,
            ApplyDailyLuckBoost: false,
            SelectedEggType: "baregg"
        );

        return new BreedingPlan(
            PlanId: "test",
            InGameDate: new DateOnly(2026, 2, 27),
            Seed: 0,
            ScopeAnimalKeys: new[] { "Chicken", "Rooster" },
            Pairs: new List<BreedingPairPlan> { pair },
            Mutations: (IReadOnlyList<BreedingMutation>)mutations,
            Diagnostics: Array.Empty<string>()
        );
    }
}
