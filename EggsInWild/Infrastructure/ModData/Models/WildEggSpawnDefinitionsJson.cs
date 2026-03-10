using System.Collections.Generic;

namespace EggsInWild.Infrastructure.ModData.Models;

public sealed class WildEggSpawnLocationJson
{
    public WildEggSpawnSizeJson? Normal { get; set; } = new();
    public WildEggSpawnSizeJson? Large { get; set; } = new();
}

public sealed class WildEggSpawnSizeJson
{
    public List<SpawnAreaRuleJson>? Common { get; set; }
    public List<SpawnAreaRuleJson>? Magic { get; set; }
}
