using WildEggsHatching.Application.Spawning;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.ModData.Models;
using Xunit;

namespace WildEggsHatching.Tests;

public sealed class SpawnChanceCalculatorTests
{
    [Fact]
    public void Calculate_UsesBaseChance_WhenEnabled()
    {
        var p = new SpawnParametersJson { BaseRandomChance = 0.2 };
        var calc = new SpawnChanceCalculator(p);

        var chance = calc.Calculate(EggKind.Wild, useBaseChance: true, ruleChance: 0.9);

        Assert.Equal(0.2, chance, 5);
    }

    [Fact]
    public void Calculate_AppliesKindModifier_AndClamps()
    {
        var p = new SpawnParametersJson { BaseRandomChance = 0.05 };
        var calc = new SpawnChanceCalculator(p);

        var chance = calc.Calculate(EggKind.LargeMagic, useBaseChance: true, ruleChance: 0.05);

        Assert.Equal(0.0, chance, 5);
    }
}
