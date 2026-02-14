using WildEggsHatching.Core.Models;

namespace WildEggsHatching.Progression.Rules;

/// <summary>
/// Interface for unlock rules that can be evaluated against a context.
/// Each rule represents a single condition that must be satisfied.
/// Rules are composable - multiple rules can be combined for complex logic.
/// </summary>
public interface IUnlockRule
{
    /// <summary>
    /// Evaluates whether this rule is satisfied by the given context.
    /// </summary>
    /// <param name="context">The unlock context containing relevant state.</param>
    /// <returns>True if the rule is satisfied.</returns>
    bool IsSatisfied(UnlockContext context);

    /// <summary>
    /// Gets a human-readable description of this rule.
    /// Useful for debugging and UI display.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the priority of this rule for ordering purposes.
    /// Lower values = higher priority.
    /// </summary>
    int Priority { get; }
}

