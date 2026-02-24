using System;
using EggsInWild.Core.Enums;
using EggsInWild.Infrastructure.ModData.Models;
using EggsInWild.Infrastructure.Config;

namespace EggsInWild.Application.Spawning;

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
            EggKind.LargeMagic => DebugKnobs.SpawnChanceModLargeMagic,
            EggKind.LargeWild => DebugKnobs.SpawnChanceModLargeWild,
            EggKind.Magic => DebugKnobs.SpawnChanceModMagic,
            _ => 0.0,
        };

        var chance = baseChance + modifier;
        return Math.Clamp(chance, 0.0, 1.0);
    }
}
