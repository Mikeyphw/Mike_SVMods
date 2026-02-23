using System.Collections.Generic;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.DesertTomb.Core.Configuration;
using WildEggsHatching.DesertTomb.Core.Spawning;
using Xunit;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.Tests;

public sealed class AllowedMonsterResolverTests
{
    [Fact]
    public void ResolveAllowedMonsters_AreaAllowlist_TakesPrecedenceOverDifficultyAllowlist()
    {
        var config = SpawnConfiguration.Default;
        config.AllowedMonstersByDifficulty = new Dictionary<TombDifficulty, HashSet<TombMonsterType>>
        {
            [TombDifficulty.Moderate] = new() { TombMonsterType.Mummy, TombMonsterType.Bat },
        };

        var resolved = AllowedMonsterResolver.ResolveAllowedMonsters(
            areaType: TombAreaType.NormalArea,
            difficulty: TombDifficulty.Moderate,
            progressionFlags: TombSpawnProgressionFlags.Disabled,
            mapTags: null,
            areaHardAllowlist: new HashSet<TombMonsterType> { TombMonsterType.Ghost },
            config: config
        );

        Assert.Contains(TombMonsterType.Ghost, resolved);
        Assert.DoesNotContain(TombMonsterType.Mummy, resolved);
        Assert.DoesNotContain(TombMonsterType.Bat, resolved);
    }

    [Fact]
    public void ResolveAllowedMonsters_GlobalBans_RemoveTypes()
    {
        var config = SpawnConfiguration.Default;
        config.Global.BannedMonsters = new HashSet<TombMonsterType> { TombMonsterType.Ghost };

        var resolved = AllowedMonsterResolver.ResolveAllowedMonsters(
            areaType: TombAreaType.NormalArea,
            difficulty: TombDifficulty.Moderate,
            progressionFlags: TombSpawnProgressionFlags.Disabled,
            mapTags: null,
            areaHardAllowlist: new HashSet<TombMonsterType> { TombMonsterType.Ghost, TombMonsterType.Mummy },
            config: config
        );

        Assert.DoesNotContain(TombMonsterType.Ghost, resolved);
        Assert.Contains(TombMonsterType.Mummy, resolved);
    }

    [Fact]
    public void ResolveAllowedMonsters_ProgressionBans_ApplyOnlyWhenEnabled()
    {
        var config = SpawnConfiguration.Default;
        config.MinimumProgressionByMonster = new Dictionary<TombMonsterType, ProgressionStep>
        {
            [TombMonsterType.RoyalSerpent] = ProgressionStep.PartIV,
        };

        var blocked = AllowedMonsterResolver.ResolveAllowedMonsters(
            areaType: TombAreaType.NormalArea,
            difficulty: TombDifficulty.Moderate,
            progressionFlags: new TombSpawnProgressionFlags(true, ProgressionStep.PartIII),
            mapTags: null,
            areaHardAllowlist: new HashSet<TombMonsterType> { TombMonsterType.RoyalSerpent, TombMonsterType.Mummy },
            config: config
        );

        Assert.DoesNotContain(TombMonsterType.RoyalSerpent, blocked);
        Assert.Contains(TombMonsterType.Mummy, blocked);

        var allowed = AllowedMonsterResolver.ResolveAllowedMonsters(
            areaType: TombAreaType.NormalArea,
            difficulty: TombDifficulty.Moderate,
            progressionFlags: new TombSpawnProgressionFlags(true, ProgressionStep.PartIV),
            mapTags: null,
            areaHardAllowlist: new HashSet<TombMonsterType> { TombMonsterType.RoyalSerpent, TombMonsterType.Mummy },
            config: config
        );

        Assert.Contains(TombMonsterType.RoyalSerpent, allowed);
        Assert.Contains(TombMonsterType.Mummy, allowed);
    }
}
