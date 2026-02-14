using WildEggsHatching.Core.Models;

namespace WildEggsHatching.Core.Events;

/// <summary>
/// Event published when the player advances to a new progression step.
/// </summary>
public sealed record PhaseUnlockedEvent(
    /// <summary>The progression step that was unlocked.</summary>
    ProgressionStep Step,

    /// <summary>The game day when the step was unlocked.</summary>
    int Day
) : IGameEvent;


