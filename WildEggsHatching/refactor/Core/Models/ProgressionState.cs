namespace WildEggsHatching.Core.Models;

/// <summary>
/// Tracks the player's progression through the wild egg unlocking story.
/// </summary>
public sealed class ProgressionState
{
    /// <summary>The current progression step the player is on.</summary>
    public ProgressionStep CurrentStep { get; private set; } = ProgressionStep.PartI;

    /// <summary>The day when the current step was reached.</summary>
    public int CurrentStepStartDay { get; private set; }

    /// <summary>
    /// Advances the progression to a new step if it's ahead of the current one.
    /// </summary>
    /// <param name="step">The step to advance to.</param>
    /// <param name="currentDay">The current game day.</param>
    public void AdvanceTo(ProgressionStep step, int currentDay)
    {
        if (step > CurrentStep)
        {
            CurrentStep = step;
            CurrentStepStartDay = currentDay;
        }
    }

    /// <summary>
    /// Checks if the player has reached a specific step.
    /// </summary>
    /// <param name="step">The step to check.</param>
    /// <returns>True if the current step is equal to or greater than the target step.</returns>
    public bool HasReached(ProgressionStep step)
    {
        return CurrentStep >= step;
    }
}
