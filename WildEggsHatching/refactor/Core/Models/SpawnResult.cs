using Microsoft.Xna.Framework;
using StardewValley;

namespace WildEggsHatching.Core.Models;

/// <summary>
/// Represents the result of a spawn attempt.
/// </summary>
public sealed class SpawnResult
{
    /// <summary>Whether an egg should spawn.</summary>
    public bool ShouldSpawn { get; private set; }

    /// <summary>The ID of the egg to spawn (null if no spawn).</summary>
    public string? EggId { get; private set; }

    /// <summary>The animal ID inside the egg (null if no spawn).</summary>
    public string? AnimalId { get; private set; }

    /// <summary>The game location for spawning (null if no spawn).</summary>
    public GameLocation? Location { get; private set; }

    /// <summary>The tile position for spawning (null if no spawn).</summary>
    public Vector2? Tile { get; private set; }

    /// <summary>The type of egg being spawned.</summary>
    public EggType EggType { get; private set; }

    /// <summary>Private constructor for no-spawn result.</summary>
    private SpawnResult()
    {
        ShouldSpawn = false;
    }

    /// <summary>Private constructor for spawn result.</summary>
    private SpawnResult(string eggId, string animalId, GameLocation location, Vector2 tile, EggType eggType)
    {
        ShouldSpawn = true;
        EggId = eggId;
        AnimalId = animalId;
        Location = location;
        Tile = tile;
        EggType = eggType;
    }

    /// <summary>Creates a result indicating no spawn.</summary>
    public static SpawnResult None() => new();

    /// <summary>Creates a result indicating a successful spawn.</summary>
    public static SpawnResult Success(string eggId, string animalId, GameLocation location, Vector2 tile, EggType eggType)
    {
        return new SpawnResult(eggId, animalId, location, tile, eggType);
    }
}


