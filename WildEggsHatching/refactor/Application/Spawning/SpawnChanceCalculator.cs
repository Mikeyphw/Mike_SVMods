using System;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.ModData.Models;

namespace WildEggsHatching.Application.Spawning;

public sealed class SpawnChanceCalculator
{
    private readonly SpawnParametersJson _parameters;

    public SpawnChanceCalculator(SpawnParametersJson parameters)
    {
        _parameters = parameters;
    }

    public double Calculate(EggKind kind, bool useBaseChance, double ruleChance)
    {
        var baseChance = useBaseChance ? _parameters.BaseRandomChance : ruleChance;
        var modifier = kind switch
        {
            EggKind.LargeMagic => -0.13,
            EggKind.LargeWild => -0.05,
            EggKind.Magic => -0.10,
            _ => 0.0,
        };

        var chance = baseChance + modifier;
        return Math.Clamp(chance, 0.0, 1.0);
    }
}
