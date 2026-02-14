using System.Collections.Generic;
using System.Linq;
using WildEggsHatching.Infrastructure.Content;

namespace WildEggsHatching.Progression.Rules;

/// <summary>
/// Evaluates a set of unlock rules against a context.
/// Supports different combination strategies (AND, OR).
/// </summary>
public sealed class UnlockRuleEvaluator
{
    /// <summary>The default evaluator instance.</summary>
    public static readonly UnlockRuleEvaluator Default = new();

    /// <summary>
    /// Evaluates all rules using AND logic - all rules must be satisfied.
    /// </summary>
    /// <param name="rules">The rules to evaluate.</param>
    /// <param name="context">The unlock context.</param>
    /// <returns>True if all rules are satisfied.</returns>
    public bool EvaluateAll(IEnumerable<IUnlockRule> rules, UnlockContext context)
    {
        if (rules == null)
        {
            return true; // No rules = all satisfied
        }

        return rules.All(rule => rule.IsSatisfied(context));
    }

    /// <summary>
    /// Evaluates rules using OR logic - at least one rule must be satisfied.
    /// </summary>
    /// <param name="rules">The rules to evaluate.</param>
    /// <param name="context">The unlock context.</param>
    /// <returns>True if at least one rule is satisfied.</returns>
    public bool EvaluateAny(IEnumerable<IUnlockRule> rules, UnlockContext context)
    {
        if (rules == null || !rules.Any())
        {
            return false; // No rules = none satisfied
        }

        return rules.Any(rule => rule.IsSatisfied(context));
    }

    /// <summary>
    /// Evaluates rules with custom combination logic.
    /// </summary>
    /// <param name="rules">The rules to evaluate.</param>
    /// <param name="context">The unlock context.</param>
    /// <param name="combinator">Function that combines boolean results.</param>
    /// <returns>The combined result of all rule evaluations.</returns>
    public bool Evaluate(
        IEnumerable<IUnlockRule> rules,
        UnlockContext context,
        System.Func<bool, bool, bool> combinator)
    {
        if (rules == null)
        {
            return true;
        }

        bool result = true;
        bool isFirst = true;

        foreach (var rule in rules)
        {
            bool ruleResult = rule.IsSatisfied(context);

            if (isFirst)
            {
                result = ruleResult;
                isFirst = false;
            }
            else
            {
                result = combinator(result, ruleResult);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the reasons why rules are not satisfied.
    /// Useful for debugging and UI feedback.
    /// </summary>
    /// <param name="rules">The rules to check.</param>
    /// <param name="context">The unlock context.</param>
    /// <returns>A list of descriptions for unsatisfied rules.</returns>
    public List<string> GetUnsatisfiedReasons(IEnumerable<IUnlockRule> rules, UnlockContext context)
    {
        var reasons = new List<string>();

        if (rules == null)
        {
            return reasons;
        }

        foreach (var rule in rules)
        {
            if (!rule.IsSatisfied(context))
            {
                reasons.Add(rule.Description);
            }
        }

        return reasons;
    }

    /// <summary>
    /// Creates a rule set for a specific progression step.
    /// </summary>
    public static IUnlockRule[] GetRulesForStep(ProgressionStep step)
    {
        return step switch
        {
            ProgressionStep.PartI => new IUnlockRule[]
            {
                // Part I: Always unlocked - no rules needed
                new TrueRule()
            },

            ProgressionStep.PartII => new IUnlockRule[]
            {
                // Part II: After completing Special Order I
                new QuestCompletedRule(WildEggConstants.QuestIds.SpecialOrderWildEggsI)
            },

            ProgressionStep.PartIII => new IUnlockRule[]
            {
                // Part III: After Wizard's study quest + 2 days
                new QuestCompletedDaysRule(WildEggConstants.QuestIds.WizardStudy, 2)
            },

            ProgressionStep.PartIV => new IUnlockRule[]
            {
                // Part IV: After Special Order II + 3 days
                new QuestCompletedDaysRule(WildEggConstants.QuestIds.SpecialOrderWildEggsII, 3)
            },

            ProgressionStep.PartV => new IUnlockRule[]
            {
                // Part V: After receiving Desert Scroll + 3 days
                new QuestCompletedDaysRule(WildEggConstants.QuestIds.TheDesertScroll, 3)
            },

            ProgressionStep.PartVI => new IUnlockRule[]
            {
                // Part VI: After Desert Scroll + 5 days (final step)
                new QuestCompletedDaysRule(WildEggConstants.QuestIds.TheDesertScroll, 5)
            },

            _ => new IUnlockRule[]
            {
                new FalseRule()
            }
        };
    }
}

