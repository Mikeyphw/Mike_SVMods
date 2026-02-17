namespace WildEggsHatching.Application.Spawning;

public enum TileRejectionReason
{
    None = 0,
    OutOfBounds = 1,
    ExcludedRect = 2,
    Terrain = 3,
    Water = 4,
    Occupied = 5,
    Impassable = 6,
}
