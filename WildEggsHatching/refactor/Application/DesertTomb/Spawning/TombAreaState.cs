using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using WildEggsHatching.DesertTomb.Core.Model;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.Application.DesertTomb.Spawning;

/// <summary>
/// Runtime state for a single tomb spawn area on a specific floor instance.
/// </summary>
public sealed class TombAreaState
{
    public int AreaId { get; }
    public TombAreaType AreaType { get; }
    public Rectangle Rect { get; }

    public IReadOnlySet<TombMonsterType> AllowedTypesResolved { get; }
    public int CapacityChosen { get; }

    /// <summary>
    /// Planned desired counts per monster type (from the pure plan).
    /// </summary>
    public IReadOnlyDictionary<TombMonsterType, int> DesiredCountByType { get; }

    /// <summary>
    /// Planned stats per monster type (from the pure plan).
    /// </summary>
    public IReadOnlyDictionary<TombMonsterType, MonsterSpawnStats> StatsByType { get; }

    public IReadOnlyList<Point> ValidSpawnTiles { get; }

    /// <summary>
    /// Consumable cache of spawn tiles for this area (persists across paced waves).
    /// </summary>
    public TombSpawnTileCache SpawnTiles { get; }

    public Dictionary<TombMonsterType, int> AliveCountByType { get; } = new();
    public Dictionary<TombMonsterType, int> SpawnedThisInstance { get; } = new();

    public TombAreaState(
        TombAreaType areaType,
        Rectangle rect,
        IReadOnlySet<TombMonsterType> allowedTypesResolved,
        int capacityChosen,
        IReadOnlyDictionary<TombMonsterType, int> desiredCountByType,
        IReadOnlyDictionary<TombMonsterType, MonsterSpawnStats> statsByType,
        IReadOnlyList<Point> validSpawnTiles,
        int minSpacingTiles
    )
    {
        AreaType = areaType;
        Rect = rect;

        AllowedTypesResolved = allowedTypesResolved ?? throw new ArgumentNullException(nameof(allowedTypesResolved));
        CapacityChosen = capacityChosen;

        DesiredCountByType = desiredCountByType ?? throw new ArgumentNullException(nameof(desiredCountByType));
        StatsByType = statsByType ?? throw new ArgumentNullException(nameof(statsByType));

        ValidSpawnTiles = validSpawnTiles ?? throw new ArgumentNullException(nameof(validSpawnTiles));
        SpawnTiles = new TombSpawnTileCache(candidates: new List<Point>(ValidSpawnTiles), minSpacingTiles: minSpacingTiles);

        AreaId = ComputeAreaId(rect, areaType);
    }

    public void IncrementSpawned(TombMonsterType type, int count = 1)
    {
        if (count <= 0)
            return;

        SpawnedThisInstance[type] = (SpawnedThisInstance.TryGetValue(type, out var current) ? current : 0) + count;
    }

    public void SetAliveCount(TombMonsterType type, int alive)
        => AliveCountByType[type] = Math.Max(0, alive);

    public static int ComputeAreaId(Rectangle rect, TombAreaType areaType)
    {
        // Stable FNV-1a hash over the key values (no allocations, deterministic).
        unchecked
        {
            const int fnvOffset = (int)2166136261;
            const int fnvPrime = 16777619;

            var hash = fnvOffset;
            hash = (hash ^ rect.X) * fnvPrime;
            hash = (hash ^ rect.Y) * fnvPrime;
            hash = (hash ^ rect.Width) * fnvPrime;
            hash = (hash ^ rect.Height) * fnvPrime;
            hash = (hash ^ (int)areaType) * fnvPrime;
            return hash;
        }
    }
}
