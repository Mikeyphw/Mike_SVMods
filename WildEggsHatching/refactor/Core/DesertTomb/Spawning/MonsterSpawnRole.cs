namespace WildEggsHatching.DesertTomb.Core.Spawning;

/// <summary>
/// High-level role assigned by the spawn planner for behavior shaping (Phase 4).
/// Execution uses this role to assign leash/home metadata to spawned monsters.
/// </summary>
public enum MonsterSpawnRole
{
    Guard = 0,
    Wander = 1,
    Patrol = 2,
}
