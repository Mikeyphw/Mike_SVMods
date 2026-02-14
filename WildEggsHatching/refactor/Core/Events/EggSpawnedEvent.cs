using Microsoft.Xna.Framework;
using StardewValley;

namespace WildEggsHatching.Core.Events;

/// <summary>
/// Event published when an egg spawns in the world.
/// </summary>
public sealed record EggSpawnedEvent(
    /// <summary>The ID of the egg that spawned.</summary>
    string EggId,

    /// <summary>The animal type inside the egg.</summary>
    string AnimalId,

    /// <summary>The game location where the egg spawned.</summary>
    GameLocation Location,

    /// <summary>The tile position where the egg spawned.</summary>
    Vector2 Tile
) : IGameEvent;


