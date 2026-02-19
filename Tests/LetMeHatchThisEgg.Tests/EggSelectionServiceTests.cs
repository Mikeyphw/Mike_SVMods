using System;
using System.Collections.Generic;
using LetMeHatchThisEgg.Core.Breeding;
using LetMeHatchThisEgg.Tests.TestDoubles;
using Xunit;

namespace LetMeHatchThisEgg.Tests;

public sealed class EggSelectionServiceTests
{
    [Theory]
    [InlineData(double.NaN, false)]
    [InlineData(-1.0, false)]
    [InlineData(0.0, false)]
    [InlineData(1.0, true)]
    [InlineData(2.0, true)]
    public void RollChance_HandlesBounds(double chance, bool expected)
    {
        var rng = new SequenceRandomProvider(doubles: new[] { 0.25 });
        var svc = new EggSelectionService(rng);

        Assert.Equal(expected, svc.RollChance(chance));
    }

    [Fact]
    public void RollChance_UsesRngForMidRange()
    {
        var rng = new SequenceRandomProvider(doubles: new[] { 0.49, 0.51 });
        var svc = new EggSelectionService(rng);

        Assert.True(svc.RollChance(0.5));
        Assert.False(svc.RollChance(0.5));
    }

    [Fact]
    public void RollChanceInclusive_InclusiveComparison()
    {
        var rng = new SequenceRandomProvider(doubles: new[] { 0.5, 0.5000001 });
        var svc = new EggSelectionService(rng);

        Assert.True(svc.RollChanceInclusive(0.5));
        Assert.False(svc.RollChanceInclusive(0.5));
    }

    [Fact]
    public void ChooseRecipient_MaleWhenChanceRollSucceeds()
    {
        var rng = new SequenceRandomProvider(doubles: new[] { 0.1, 0.9 });
        var svc = new EggSelectionService(rng);

        Assert.Equal(EggRecipient.Male, svc.ChooseRecipient(0.5));
        Assert.Equal(EggRecipient.Female, svc.ChooseRecipient(0.5));
    }

    [Fact]
    public void RollWeightedOrDefault_ReturnsFallbackWhenEmpty()
    {
        var rng = new SequenceRandomProvider(doubles: new[] { 0.2 });
        var svc = new EggSelectionService(rng);

        Assert.Equal("fallback", svc.RollWeightedOrDefault(Array.Empty<(string, double)>(), "fallback"));
    }

    [Fact]
    public void RollWeightedOrDefault_PrefersHigherWeightGivenDeterministicRng()
    {
        // WeightedTable uses NextDouble internally; with 0.99 it should land near the end of cumulative weight.
        var rng = new SequenceRandomProvider(doubles: new[] { 0.99 });
        var svc = new EggSelectionService(rng);

        var entries = new List<(string item, double weight)>
        {
            ("low", 1),
            ("high", 100),
        };

        var result = svc.RollWeightedOrDefault(entries, "fallback");
        Assert.Equal("high", result);
    }
}
