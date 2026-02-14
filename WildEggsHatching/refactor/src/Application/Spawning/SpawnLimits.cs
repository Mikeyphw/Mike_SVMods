using System;
using System.Collections.Generic;
using StardewValley;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.ModData.Models;

namespace WildEggsHatching.Application.Spawning;

public sealed class SpawnLimits
{
    public SpawnLimits() { }

    private readonly Dictionary<string, int> _mapAny = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _mapMagic = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _mapLarge = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<EggKind, int> _worldByKind = new();

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

        var worldCap = GetWorldCap(kind, p);
        var worldCount = _worldByKind.TryGetValue(kind, out var c) ? c : 0;
        if (worldCount >= worldCap)
        {
            reason = $"world cap hit ({kind}:{worldCount}/{worldCap})";
            return false;
        }

        var anyCap = Math.Max(0, p.MaxEggPerMap_Any);
        var effective = anyCap > 0 ? anyCap : int.MaxValue;

        if (kind.IsMagic())
        {
            var magicCap = Math.Max(0, p.MaxEggPerMap_Magic);
            if (magicCap > 0)
                effective = Math.Min(effective, magicCap);
        }

        if (kind.IsLarge())
        {
            var largeCap = Math.Max(0, p.MaxEggPerMap_Large);
            if (largeCap > 0)
                effective = Math.Min(effective, largeCap);
        }

        var mapCount = _mapAny.TryGetValue(mapName, out var mc) ? mc : 0;
        if (mapCount >= effective)
        {
            reason = $"map cap hit ({mapName}:{mapCount}/{effective})";
            return false;
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

        _worldByKind[kind] = (_worldByKind.TryGetValue(kind, out var w) ? w : 0) + 1;
    }

    private static bool IsWildEgg(string itemId)
        => itemId.StartsWith("Mikew.wildeggshatching_", StringComparison.OrdinalIgnoreCase)
           && itemId.EndsWith("Egg", StringComparison.OrdinalIgnoreCase);

    private static int GetWorldCap(EggKind kind, SpawnParametersJson p)
    {
        var key = kind.ToSpawnParametersKey();
        if (p.MaxWorldEggs is not null && p.MaxWorldEggs.TryGetValue(key, out var cap) && cap > 0)
            return cap;

        return int.MaxValue;
    }
}
