using System;
using System.Collections.Generic;

namespace RoosterControl.Application.Produces;

/// <summary>
/// Applies parsed produce override rules to a 4-slot candidate table, producing an effective table plus diagnostics.
/// This is a pure component: it does not depend on SMAPI/game types.
/// </summary>
public static class ProduceOverrideApplicator
{
    /// <summary>Apply overrides to a candidate table.</summary>
    /// <param name="ruleSet">Rules to evaluate in their original list order.</param>
    /// <param name="context">Runtime context used for matching <c>when</c> clauses.</param>
    /// <param name="candidates">A candidate table (typically 4 slots).</param>
    public static ProduceOverrideApplyResult Apply(
        ProduceOverrideRuleSet ruleSet,
        ProduceOverrideContext context,
        IReadOnlyList<ProduceOverrideCandidate> candidates
    )
    {
        if (ruleSet is null) throw new ArgumentNullException(nameof(ruleSet));
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (candidates is null) throw new ArgumentNullException(nameof(candidates));

        var table = NormalizeCandidates(candidates);

        var applied = new List<ProduceOverrideAppliedRule>();
        var issues = new List<ProduceOverrideApplyIssue>();

        foreach (var rule in ruleSet.Rules)
        {
            if (!Matches(rule.When, context))
                continue;

            var effects = new List<ProduceOverrideEffect>();

            ApplyExcludeKeys(rule.Then.ExcludeKeys, table, effects);
            ApplyReplaceKeys(rule.Then.ReplaceKeys, table, effects);
            ConsolidateDuplicates(table, effects);
            ApplyWeightMultipliers(rule.Then.WeightMultipliers, table, effects, issues);
            ApplyHardOverride(rule.Then.HardOverrideKey, table, effects, issues);

            applied.Add(
                new ProduceOverrideAppliedRule(
                    OrderIndex: rule.OrderIndex,
                    Id: rule.Id,
                    Effects: effects
                )
            );
        }

        return new ProduceOverrideApplyResult(table, applied, issues);
    }

    private static ProduceOverrideCandidate[] NormalizeCandidates(IReadOnlyList<ProduceOverrideCandidate> candidates)
    {
        var len = candidates.Count;
        var result = new ProduceOverrideCandidate[Math.Max(4, len)];

        for (var i = 0; i < result.Length; i++)
        {
            if (i < len)
            {
                var c = candidates[i];
                var key = (c.Key ?? string.Empty).Trim();
                var w = Math.Clamp(c.Weight, 0, 100);
                result[i] = new ProduceOverrideCandidate(key, w);
            }
            else
            {
                result[i] = new ProduceOverrideCandidate(string.Empty, 0);
            }
        }

        return result;
    }

    private static bool Matches(ProduceOverrideWhen when, ProduceOverrideContext ctx)
    {
        if (when is null) return true;

        if (!MatchesToken(when.RoosterType, NormalizeRoosterType(ctx.RoosterType)))
            return false;
        if (!MatchesToken(when.BuildingType, NormalizeToken(ctx.BuildingType)))
            return false;
        if (!MatchesToken(when.LocationName, NormalizeToken(ctx.LocationName)))
            return false;
        if (!MatchesToken(when.FarmAnimalName, NormalizeToken(ctx.FarmAnimalName)))
            return false;

        if (when.Day is not null && when.Day.Value != ctx.Day)
            return false;

        if (when.ProduceType is not null && when.ProduceType.Value != ctx.ProduceType)
            return false;

        return true;
    }

    private static bool MatchesToken(string? ruleToken, string ctxTokenNormalized)
    {
        if (string.IsNullOrWhiteSpace(ruleToken))
            return true;

        var ruleNorm = NormalizeToken(ruleToken);

        return ruleNorm.Equals(ctxTokenNormalized, StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyExcludeKeys(IReadOnlyList<string> excludeKeys, ProduceOverrideCandidate[] table, List<ProduceOverrideEffect> effects)
    {
        if (excludeKeys.Count == 0)
            return;

        for (var i = 0; i < table.Length; i++)
        {
            var c = table[i];
            if (c.Weight <= 0 || string.IsNullOrWhiteSpace(c.Key))
                continue;

            foreach (var ex in excludeKeys)
            {
                if (string.Equals(c.Key, ex, StringComparison.OrdinalIgnoreCase))
                {
                    effects.Add(new ProduceOverrideEffect(ProduceOverrideEffectKind.ExcludeKey, c.Key, SlotIndex: i, BeforeWeight: c.Weight, AfterWeight: 0));
                    table[i] = c with { Weight = 0 };
                    break;
                }
            }
        }
    }

    private static void ApplyReplaceKeys(IReadOnlyDictionary<string, string> replaceKeys, ProduceOverrideCandidate[] table, List<ProduceOverrideEffect> effects)
    {
        if (replaceKeys.Count == 0)
            return;

        for (var i = 0; i < table.Length; i++)
        {
            var c = table[i];
            if (c.Weight <= 0 || string.IsNullOrWhiteSpace(c.Key))
                continue;

            if (replaceKeys.TryGetValue(c.Key, out var toKey))
            {
                toKey ??= string.Empty;
                toKey = toKey.Trim();

                effects.Add(new ProduceOverrideEffect(ProduceOverrideEffectKind.ReplaceKey, c.Key, SlotIndex: i, Value: toKey, BeforeWeight: c.Weight, AfterWeight: c.Weight));
                table[i] = c with { Key = toKey };
            }
        }
    }

    private static void ConsolidateDuplicates(ProduceOverrideCandidate[] table, List<ProduceOverrideEffect> effects)
    {
        var firstIndexByKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < table.Length; i++)
        {
            var c = table[i];
            if (c.Weight <= 0 || string.IsNullOrWhiteSpace(c.Key))
                continue;

            if (firstIndexByKey.TryGetValue(c.Key, out var first))
            {
                var firstC = table[first];
                var merged = Math.Clamp(firstC.Weight + c.Weight, 0, 100);

                effects.Add(
                    new ProduceOverrideEffect(
                        ProduceOverrideEffectKind.MergeDuplicate,
                        c.Key,
                        SlotIndex: i,
                        Value: $"into:{first}",
                        BeforeWeight: c.Weight,
                        AfterWeight: 0
                    )
                );

                effects.Add(
                    new ProduceOverrideEffect(
                        ProduceOverrideEffectKind.MergeDuplicate,
                        firstC.Key,
                        SlotIndex: first,
                        Value: $"from:{i}",
                        BeforeWeight: firstC.Weight,
                        AfterWeight: merged
                    )
                );

                table[first] = firstC with { Weight = merged };
                table[i] = c with { Weight = 0 };
            }
            else
            {
                firstIndexByKey[c.Key] = i;
            }
        }
    }

    private static void ApplyWeightMultipliers(
        IReadOnlyDictionary<string, double> multipliers,
        ProduceOverrideCandidate[] table,
        List<ProduceOverrideEffect> effects,
        List<ProduceOverrideApplyIssue> issues
    )
    {
        if (multipliers.Count == 0)
            return;

        foreach (var (key, mult) in multipliers)
        {
            if (string.IsNullOrWhiteSpace(key))
                continue;

            if (double.IsNaN(mult) || double.IsInfinity(mult) || mult <= 0)
            {
                issues.Add(
                    new ProduceOverrideApplyIssue(
                        ProduceOverrideApplyIssueSeverity.Warning,
                        "invalid_multiplier",
                        $"Multiplier for key '{key}' must be a finite positive number; got '{mult}'.",
                        key
                    )
                );
                continue;
            }

            for (var i = 0; i < table.Length; i++)
            {
                var c = table[i];
                if (c.Weight <= 0 || string.IsNullOrWhiteSpace(c.Key))
                    continue;

                if (!string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase))
                    continue;

                var before = c.Weight;
                var after = (int)Math.Round(before * mult, MidpointRounding.AwayFromZero);
                after = Math.Clamp(after, 0, 100);

                if (after != before)
                {
                    effects.Add(new ProduceOverrideEffect(ProduceOverrideEffectKind.WeightMultiplier, c.Key, SlotIndex: i, Multiplier: mult, BeforeWeight: before, AfterWeight: after));
                    table[i] = c with { Weight = after };
                }
            }
        }
    }

    private static void ApplyHardOverride(
        string? hardOverrideKey,
        ProduceOverrideCandidate[] table,
        List<ProduceOverrideEffect> effects,
        List<ProduceOverrideApplyIssue> issues
    )
    {
        if (string.IsNullOrWhiteSpace(hardOverrideKey))
            return;

        hardOverrideKey = hardOverrideKey.Trim();
        if (hardOverrideKey.Length == 0)
        {
            issues.Add(
                new ProduceOverrideApplyIssue(
                    ProduceOverrideApplyIssueSeverity.Warning,
                    "empty_hard_override",
                    "Hard override key was empty after trimming.",
                    "hardOverrideKey"
                )
            );
            return;
        }

        var foundIndex = -1;
        for (var i = 0; i < table.Length; i++)
        {
            if (string.Equals(table[i].Key, hardOverrideKey, StringComparison.OrdinalIgnoreCase))
            {
                foundIndex = i;
                break;
            }
        }

        if (foundIndex < 0)
        {
            // Force slot 0 to the override key deterministically.
            table[0] = new ProduceOverrideCandidate(hardOverrideKey, 100);
            foundIndex = 0;
            effects.Add(new ProduceOverrideEffect(ProduceOverrideEffectKind.HardOverride, hardOverrideKey, SlotIndex: 0, BeforeWeight: null, AfterWeight: 100));
        }
        else
        {
            effects.Add(new ProduceOverrideEffect(ProduceOverrideEffectKind.HardOverride, hardOverrideKey, SlotIndex: foundIndex, BeforeWeight: table[foundIndex].Weight, AfterWeight: 100));
        }

        for (var i = 0; i < table.Length; i++)
        {
            if (i == foundIndex)
            {
                table[i] = table[i] with { Weight = 100 };
            }
            else if (table[i].Weight != 0)
            {
                effects.Add(new ProduceOverrideEffect(ProduceOverrideEffectKind.HardOverrideZeroOthers, table[i].Key, SlotIndex: i, BeforeWeight: table[i].Weight, AfterWeight: 0));
                table[i] = table[i] with { Weight = 0 };
            }
        }
    }

    private static string NormalizeRoosterType(string? s)
    {
        var t = NormalizeToken(s);
        if (t.EndsWith("rooster", StringComparison.OrdinalIgnoreCase))
            t = t[..^"rooster".Length].Trim();
        return t;
    }

    private static string NormalizeToken(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return string.Empty;

        var chars = s.Trim().ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] == '_' || chars[i] == '-')
                chars[i] = ' ';
        }

        var normalized = new string(chars);

        // Collapse repeated whitespace deterministically.
        var result = new List<char>(normalized.Length);
        var prevSpace = false;
        foreach (var ch in normalized)
        {
            var isSpace = char.IsWhiteSpace(ch);
            if (isSpace)
            {
                if (!prevSpace)
                    result.Add(' ');
                prevSpace = true;
            }
            else
            {
                result.Add(ch);
                prevSpace = false;
            }
        }

        return new string(result.ToArray()).Trim();
    }
}

/// <summary>Context used by <see cref="ProduceOverrideApplicator"/> to evaluate rule match conditions.</summary>
public sealed record ProduceOverrideContext(
    string? RoosterType,
    string? BuildingType,
    string? LocationName,
    string? FarmAnimalName,
    int Day,
    ProduceOverrideProduceType ProduceType
);

/// <summary>One candidate entry in a 4-slot produce table.</summary>
public readonly record struct ProduceOverrideCandidate(string Key, int Weight);

/// <summary>Application result including effective candidates and diagnostics.</summary>
public sealed record ProduceOverrideApplyResult(
    IReadOnlyList<ProduceOverrideCandidate> EffectiveCandidates,
    IReadOnlyList<ProduceOverrideAppliedRule> AppliedRules,
    IReadOnlyList<ProduceOverrideApplyIssue> Issues
);

public sealed record ProduceOverrideAppliedRule(
    int OrderIndex,
    string? Id,
    IReadOnlyList<ProduceOverrideEffect> Effects
);

public enum ProduceOverrideEffectKind
{
    ExcludeKey,
    ReplaceKey,
    MergeDuplicate,
    WeightMultiplier,
    HardOverride,
    HardOverrideZeroOthers
}

public sealed record ProduceOverrideEffect(
    ProduceOverrideEffectKind Kind,
    string Key,
    int SlotIndex,
    string? Value = null,
    double? Multiplier = null,
    int? BeforeWeight = null,
    int? AfterWeight = null
);

public enum ProduceOverrideApplyIssueSeverity
{
    Info,
    Warning,
    Error
}

public sealed record ProduceOverrideApplyIssue(
    ProduceOverrideApplyIssueSeverity Severity,
    string Code,
    string Message,
    string KeyOrPath
);
