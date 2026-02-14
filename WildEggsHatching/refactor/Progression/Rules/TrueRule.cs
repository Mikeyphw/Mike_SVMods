namespace WildEggsHatching.Progression.Rules;

/// <summary>
/// A rule that is always satisfied.
/// Useful as a base rule or for steps with no requirements.
/// </summary>
public sealed class TrueRule : IUnlockRule
{
    /// <summary>
    /// Always returns true.
    /// </summary>
    public bool IsSatisfied(UnlockContext context) => true;

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string Description => "Always satisfied";

    /// <summary>
    /// Priority for ordering (highest - this is a terminal rule).
    /// </summary>
    public int Priority => int.MaxValue;

    /// <summary>
    /// Returns a string representation.
    /// </summary>
    public override string ToString() => "TrueRule";
}

