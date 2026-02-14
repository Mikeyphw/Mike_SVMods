using StardewValley;
using WildEggsHatching.Core.Enums;

namespace WildEggsHatching.Progression.Rules;

/// <summary>
/// Rule that requires a minimum number of days to have passed.
/// </summary>
public sealed class MinDaysRule : IUnlockRule
{
    /// <summary>The minimum number of days required.</summary>
    private readonly int _minDays;

    /// <summary>The reference day to count from (defaults to day 0, the start of the game).</summary>
    private readonly int _referenceDay;

    /// <summary>
    /// Creates a new minimum days rule.
    /// </summary>
    /// <param name="minDays">The minimum number of days that must have passed.</param>
    /// <param name="referenceDay">The day to count from. Use 0 for game start.</param>
    public MinDaysRule(int minDays, int referenceDay = 0)
    {
        _minDays = minDays;
        _referenceDay = referenceDay;
    }

    /// <summary>
    /// Creates a new minimum days rule relative to game start.
    /// </summary>
    /// <param name="minDays">The minimum number of days since game start.</param>
    public static MinDaysRule SinceGameStart(int minDays) => new(minDays, 0);

    /// <summary>
    /// Creates a new minimum days rule relative to current step.
    /// </summary>
    /// <param name="minDays">The minimum number of days since entering current step.</param>
    /// <param name="currentStep">The step to track days from.</param>
    public static MinDaysRule SinceStep(int minDays, ProgressionStep currentStep) => new(minDays);

    /// <summary>
    /// Evaluates whether the minimum days requirement is met.
    /// </summary>
    public bool IsSatisfied(UnlockContext context)
    {
        int daysPassed = context.CurrentDay - _referenceDay;
        return daysPassed >= _minDays;
    }

    /// <summary>
    /// Gets a human-readable description of this rule.
    /// </summary>
    public string Description => $"At least {_minDays} days have passed";

    /// <summary>
    /// Gets the priority of this rule.
    /// </summary>
    public int Priority => 10; // Low priority - usually a base requirement

    /// <summary>
    /// Returns a string representation of this rule.
    /// </summary>
    public override string ToString()
    {
        return $"MinDaysRule({_minDays} days, from day {_referenceDay})";
    }
}

