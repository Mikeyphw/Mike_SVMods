using System;
using System.Collections.Generic;
using System.Linq;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.DesertTomb.Core.Configuration;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.DesertTomb.Core.Spawning;

/// <summary>
/// Pure policy for resolving which monster types are allowed for an area.
///
/// Precedence:
/// 1) Area hard-allowlist (layout or map override) if present
/// 2) Difficulty allowlist
/// 3) Global bans (config)
/// 4) Progression bans (config)
/// 5) Map tag bans (optional; reserved for later map-driven rules)
/// </summary>
public static class AllowedMonsterResolver
{
    public static IReadOnlySet<TombMonsterType> ResolveAllowedMonsters(
        TombAreaType areaType,
        TombDifficulty difficulty,
        TombSpawnProgressionFlags progressionFlags,
        IReadOnlySet<string>? mapTags,
        IReadOnlySet<TombMonsterType> areaHardAllowlist,
        SpawnConfiguration config
    )
    {
        if (config is null)
            throw new ArgumentNullException(nameof(config));
        if (areaHardAllowlist is null)
            throw new ArgumentNullException(nameof(areaHardAllowlist));

        HashSet<TombMonsterType> allowed;

        if (areaHardAllowlist.Count > 0)
        {
            allowed = new HashSet<TombMonsterType>(areaHardAllowlist);
        }
        else if (config.AllowedMonstersByDifficulty.TryGetValue(difficulty, out var byDifficulty) && byDifficulty.Count > 0)
        {
            allowed = new HashSet<TombMonsterType>(byDifficulty);
        }
        else
        {
            allowed = Enum.GetValues(typeof(TombMonsterType))
                .Cast<TombMonsterType>()
                .ToHashSet();
        }

        if (config.Global.BannedMonsters.Count > 0)
            allowed.ExceptWith(config.Global.BannedMonsters);

        if (progressionFlags.ProgressionModeEnabled && config.MinimumProgressionByMonster.Count > 0)
        {
            foreach (var (monster, minStep) in config.MinimumProgressionByMonster)
            {
                if (progressionFlags.Step < minStep)
                    allowed.Remove(monster);
            }
        }

        if (mapTags is not null && mapTags.Count > 0)
        {
            if (mapTags.Contains("NoSerpents"))
                allowed.Remove(TombMonsterType.RoyalSerpent);
            if (mapTags.Contains("NoSkulls"))
                allowed.Remove(TombMonsterType.HauntedSkull);
            if (mapTags.Contains("NoSpiders"))
                allowed.Remove(TombMonsterType.Spider);
        }

        _ = areaType; // reserved for implicit area rules later

        return allowed;
    }
}
