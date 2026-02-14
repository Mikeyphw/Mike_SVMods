namespace WildEggsHatching.Progression.Rules;

/// <summary>
/// A rule that is never satisfied.
/// Useful for undefined steps or as a fail-safe.
/// </summary>
public sealed class FalseRule : IUnlockRule
{
    /// <summary>
    /// Always returns false.
    /// </summary>
    public bool IsSatisfied(UnlockContext context) => false;

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string Description => "Never satisfied";

    /// <summary>
    /// Priority for ordering (lowest - this is a terminal rule).
    /// </summary>
    public int Priority => int.MinValue;

    /// <summary>
    /// Returns a string representation.
    /// </summary>
    public override string ToString() => "FalseRule";
}

