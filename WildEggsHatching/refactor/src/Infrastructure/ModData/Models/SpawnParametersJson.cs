using System.Collections.Generic;

namespace WildEggsHatching.Infrastructure.ModData.Models;

public sealed class SpawnParametersJson
{
    public int MaxSpawningTries { get; set; } = 400;
    public int MaxEggPerMap_Any { get; set; } = 3;
    public int MaxEggPerMap_Magic { get; set; } = 1;
    public int MaxEggPerMap_Large { get; set; } = 2;
    public double BaseRandomChance { get; set; } = 0.15;

    public Dictionary<string, int> MaxWorldEggs { get; set; } = new();

    public ParameterPackJson Parameters_Packs { get; set; } = new();
    public ParameterPackJson Parameters_Vanilla { get; set; } = new();
}

public sealed class ParameterPackJson
{
    public int MinEggArea { get; set; } = 1;
    public int MaxEggArea { get; set; } = 2;
    public int MaxEggPack { get; set; } = 5;
    public int CertainEggs { get; set; } = 2;
    public bool UseJsonValues { get; set; } = true;
    public int MaxEggs_All { get; set; } = 5;
    public int MinEggs { get; set; } = 1;
}
