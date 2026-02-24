using System;
using System.Collections.Generic;
using StardewValley;
using EggsInWild.Core.Enums;
using EggsInWild.Infrastructure.ModData.Models;

namespace EggsInWild.Application.Spawning;

/// <summary>
/// Tracks map-level egg counts for enforcing legacy per-map caps.
/// World and per-type world caps are enforced by the spawner service (per-day semantics).
/// </summary>
public sealed class SpawnLimits
{
    private readonly Dictionary<string, int> _mapAny = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _mapMagic = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _mapLarge = new(StringComparer.OrdinalIgnoreCase);

    public static SpawnLimits SnapshotFromWorld(IEnumerable<GameLocation> locations)
    {
        var limits = new SpawnLimits();

        foreach (var loc in locations)
        {
            if (loc is null)
                continue;

            foreach (var pair in loc.objects.Pairs)
            {
                if (pair.Value is not StardewValley.Object obj)
                    continue;

                var id = obj.ItemId;
                if (!IsWildEgg(id))
                    continue;

                var kind = EggKindExtensions.FromEggItemId(id);
                limits.RecordSpawn(loc.NameOrUniqueName, kind);
            }
        }

        return limits;
    }

    public bool CanSpawnInMap(string mapName, EggKind kind, SpawnParametersJson p, out string reason)
    {
        reason = "";

        var anyCap = Math.Max(0, p.MaxEggPerMap_Any);
        if (anyCap > 0)
        {
            var anyCount = _mapAny.TryGetValue(mapName, out var a) ? a : 0;
            if (anyCount >= anyCap)
            {
                reason = $"map any cap hit ({mapName}:{anyCount}/{anyCap})";
                return false;
            }
        }

        if (kind.IsMagic())
        {
            var magicCap = Math.Max(0, p.MaxEggPerMap_Magic);
            if (magicCap > 0)
            {
                var magicCount = _mapMagic.TryGetValue(mapName, out var m) ? m : 0;
                if (magicCount >= magicCap)
                {
                    reason = $"map magic cap hit ({mapName}:{magicCount}/{magicCap})";
                    return false;
                }
            }
        }

        if (kind.IsLarge())
        {
            var largeCap = Math.Max(0, p.MaxEggPerMap_Large);
            if (largeCap > 0)
            {
                var largeCount = _mapLarge.TryGetValue(mapName, out var l) ? l : 0;
                if (largeCount >= largeCap)
                {
                    reason = $"map large cap hit ({mapName}:{largeCount}/{largeCap})";
                    return false;
                }
            }
        }

        return true;
    }

    public void RecordSpawn(string mapName, EggKind kind)
    {
        _mapAny[mapName] = (_mapAny.TryGetValue(mapName, out var a) ? a : 0) + 1;

        if (kind.IsMagic())
            _mapMagic[mapName] = (_mapMagic.TryGetValue(mapName, out var m) ? m : 0) + 1;

        if (kind.IsLarge())
            _mapLarge[mapName] = (_mapLarge.TryGetValue(mapName, out var l) ? l : 0) + 1;
    }

    private static bool IsWildEgg(string itemId)
        => itemId.StartsWith("Mikew.eggsinwild_", StringComparison.OrdinalIgnoreCase)
           && itemId.EndsWith("Egg", StringComparison.OrdinalIgnoreCase);

    public static int GetPerKindWorldCap(EggKind kind, SpawnParametersJson p)
    {
        var key = kind.ToSpawnParametersKey();
        if (p.MaxWorldEggs is not null && p.MaxWorldEggs.TryGetValue(key, out var cap) && cap > 0)
            return cap;

        return int.MaxValue;
    }
}
