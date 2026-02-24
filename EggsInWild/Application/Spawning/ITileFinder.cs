using Microsoft.Xna.Framework;
using Mikew.NestCore.Framework.Random;
using StardewValley;

namespace EggsInWild.Application.Spawning;

public interface ITileFinder
{
    Vector2? FindTile(GameLocation location, SpawnJob job, IRandomProvider random, int maxAttempts, out TileSearchStats stats);
}

/// <summary>
/// Per-search counters used for diagnosis (and histogram aggregation).
/// </summary>
public sealed record TileSearchStats(
    int Attempts,
    int RejectedOutOfBounds,
    int RejectedExcludedRect,
    int RejectedTerrain,
    int RejectedWater,
    int RejectedOccupied,
    int RejectedImpassable);
