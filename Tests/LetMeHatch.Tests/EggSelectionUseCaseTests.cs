using LetMeHatch.Application.UseCases;
using LetMeHatch.Tests.TestDoubles;
using Xunit;

namespace LetMeHatch.Tests;

public sealed class EggSelectionUseCaseTests
{
    [Fact]
    public void RollChance_UsesUnderlyingRng()
    {
        var rng = new SequenceRandomProvider(0.24, 0.25);
        var uc = new EggSelectionUseCase(rng);

        Assert.True(uc.RollChance(0.25));
        Assert.False(uc.RollChance(0.25));
    }

    [Fact]
    public void RollWeightedOrDefault_WhenEmpty_ReturnsFallback()
    {
        var rng = new SequenceRandomProvider(0.5);
        var uc = new EggSelectionUseCase(rng);

        var result = uc.RollWeightedOrDefault(new (string item, double weight)[] { }, fallback: "fallback");
        Assert.Equal("fallback", result);
    }
}
