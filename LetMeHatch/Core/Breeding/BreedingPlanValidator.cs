using System;
using System.Collections.Generic;
using System.Linq;

namespace LetMeHatch.Core.Breeding;

/// <summary>
/// Validates invariants for a <see cref="BreedingPlan"/> before it is applied.
/// </summary>
public sealed class BreedingPlanValidator
{
    public BreedingPlanValidationResult Validate(BreedingPlan plan)
    {
        if (plan is null) throw new ArgumentNullException(nameof(plan));

        var errors = new List<string>();
        var mutations = plan.Mutations ?? Array.Empty<BreedingMutation>();
        var pairs = plan.Pairs ?? Array.Empty<BreedingPairPlan>();

        ValidateEnforcementOrdering(mutations, errors);

        var enforced = new HashSet<string>(
            mutations.OfType<BreedingMutation.EnforceMaleNeverLays>().Select(m => m.MaleKey),
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var pair in pairs)
        {
            if (string.IsNullOrWhiteSpace(pair.FemaleAnimalKey))
            {
                errors.Add("Pair has empty FemaleAnimalKey.");
            }

            if (string.IsNullOrWhiteSpace(pair.MaleAnimalKey))
            {
                errors.Add($"Pair for female '{pair.FemaleAnimalKey}' has empty MaleAnimalKey.");
                continue;
            }

            if (!enforced.Contains(pair.MaleAnimalKey))
            {
                errors.Add($"Missing EnforceMaleNeverLays mutation for male '{pair.MaleAnimalKey}'.");
            }

            foreach (var other in pair.OtherMaleKeys ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(other) && !enforced.Contains(other))
                {
                    errors.Add($"Missing EnforceMaleNeverLays mutation for other male '{other}'.");
                }
            }
        }

        var maleKeys = enforced;
        foreach (var rebuild in mutations.OfType<BreedingMutation.RebuildFemaleEggDomain>())
        {
            if (maleKeys.Contains(rebuild.FemaleKey))
            {
                errors.Add($"RebuildFemaleEggDomain targets male key '{rebuild.FemaleKey}'.");
            }
        }

        foreach (var ensure in mutations.OfType<BreedingMutation.EnsureCloneExists>())
        {
            if (string.IsNullOrWhiteSpace(ensure.OriginalKey) || string.IsNullOrWhiteSpace(ensure.CloneKey))
            {
                errors.Add("EnsureCloneExists has empty OriginalKey or CloneKey.");
            }
        }

        return errors.Count == 0
            ? BreedingPlanValidationResult.Valid()
            : BreedingPlanValidationResult.Invalid(errors);
    }

    public void ValidateOrThrow(BreedingPlan plan)
    {
        var result = Validate(plan);
        if (result.IsValid)
        {
            return;
        }

        throw new InvalidOperationException(
            "BreedingPlan validation failed:\n" + string.Join("\n", result.Errors.Select(e => "- " + e))
        );
    }

    private static void ValidateEnforcementOrdering(IReadOnlyList<BreedingMutation> mutations, List<string> errors)
    {
        var firstEnforce = -1;

        for (var i = 0; i < mutations.Count; i++)
        {
            if (mutations[i] is BreedingMutation.EnforceMaleNeverLays)
            {
                firstEnforce = i;
                break;
            }
        }

        if (firstEnforce < 0)
        {
            return;
        }

        for (var i = firstEnforce; i < mutations.Count; i++)
        {
            if (mutations[i] is not BreedingMutation.EnforceMaleNeverLays)
            {
                errors.Add("EnforceMaleNeverLays mutations must be contiguous and appear at the end of the mutation list.");
                break;
            }
        }
    }
}

/// <summary>
/// Validation result for a <see cref="BreedingPlan"/>.
/// </summary>
public sealed record BreedingPlanValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static BreedingPlanValidationResult Valid() => new(true, Array.Empty<string>());

    public static BreedingPlanValidationResult Invalid(IReadOnlyList<string> errors)
        => new(false, errors ?? Array.Empty<string>());
}
