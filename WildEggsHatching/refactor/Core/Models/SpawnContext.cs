using Microsoft.Xna.Framework;
using StardewValley;
using WildEggsHatching.Core.Enums;

namespace WildEggsHatching.Core.Models;

/// <summary>
/// Contains all the context information needed to make a spawn decision.
/// </summary>
public sealed class SpawnContext
{
    /// <summary>The base spawn chance from configuration (default: 0.15).</summary>
    public float BaseChance { get; init; }

    /// <summary>The type of egg being spawned.</summary>
    public EggType EggType { get; init; }

    /// <summary>The current daily luck state.</summary>
    public LuckyTypes DailyLuck { get; init; }

    /// <summary>The current progression step.</summary>
    public ProgressionStep CurrentStep { get; init; }

    /// <summary>The current game day.</summary>
    public int CurrentDay { get; init; }

    /// <summary>The mine level (for weighted RNG seed).</summary>
    public int MineLevel { get; init; }

    /// <summary>The game location where spawning is being considered.</summary>
    public GameLocation Location { get; init; } = null!;

    /// <summary>The tile position where spawning is being considered.</summary>
    public Vector2 Tile { get; init; }

    /// <summary>The current spawn area configuration.</summary>
    public Area? Area { get; init; }

    /// <summary>Dictionary of eggs already spawned in the area.</summary>
    public Dictionary<string, int>? SpawnedEggsInArea { get; init; }

    /// <summary>Dictionary of eggs already spawned in the location.</summary>
    public Dictionary<string, int>? SpawnedEggsInLocation { get; init; }
}


