using StardewValley;

namespace WildEggsHatching.Core.Events;

/// <summary>
/// Event published when a wild egg hatches into an animal.
/// </summary>
public sealed record AnimalHatchedEvent(
    /// <summary>The ID of the animal that hatched.</summary>
    string AnimalId,

    /// <summary>The ID of the parent egg.</summary>
    string EggId,

    /// <summary>The game location where the hatching occurred.</summary>
    GameLocation Location,

    /// <summary>The tile position where the animal hatched.</summary>
    Microsoft.Xna.Framework.Vector2 Tile
) : IGameEvent;


