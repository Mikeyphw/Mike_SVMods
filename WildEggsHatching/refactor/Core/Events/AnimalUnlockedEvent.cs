using WildEggsHatching.Core.Models;

namespace WildEggsHatching.Core.Events;

/// <summary>
/// Event published when a new animal is unlocked for the player.
/// </summary>
public sealed record AnimalUnlockedEvent(
    /// <summary>The ID of the animal that was unlocked.</summary>
    string AnimalId,

    /// <summary>The method by which the animal was unlocked (e.g., "EggHatched", "NoteRead").</summary>
    string UnlockReason,

    /// <summary>The game day when the unlock occurred.</summary>
    int Day
) : IGameEvent;


