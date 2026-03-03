using EggsInWild.Application.Spawning;
using EggsInWild.Core.Enums;
using EggsInWild.Infrastructure.ModData.Models;
using Xunit;

namespace EggsInWild.Tests;

public sealed class SpawnLimitsTests
{
    [Fact]
    public void CanSpawn_Blocks_WhenMapCapReached()
    {
        var p = new SpawnParametersJson
        {
            MaxEggPerMap_Any = 1,
            MaxEggPerMap_Large = 5,
            MaxEggPerMap_Magic = 5,
            MaxWorldEggs = { ["wildEgg"] = 99 }
        };

        var limits = new SpawnLimits();
        limits.RecordSpawn("Forest", EggKind.Wild);

        var ok = limits.CanSpawnInMap("Forest", EggKind.Wild, p, out _);

        Assert.False(ok);
    }

    [Fact]
    public void CanSpawn_Blocks_WhenWorldCapReached()
    {
        var p = new SpawnParametersJson
        {
            MaxEggPerMap_Any = 99,
            MaxWorldEggs = { ["magicWildEgg"] = 1 }
        };

        var limits = new SpawnLimits();
        limits.RecordSpawn("Forest", EggKind.Magic);

        var ok = limits.CanSpawnInMap("Forest", EggKind.Magic, p, out var reason);

        Assert.False(ok);
        Assert.Contains("map magic cap hit", reason);
    }
}
