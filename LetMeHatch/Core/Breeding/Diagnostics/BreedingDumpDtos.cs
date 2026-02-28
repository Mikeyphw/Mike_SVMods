using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace LetMeHatch.Core.Breeding.Diagnostics;

/// <summary>
/// Stable JSON DTOs for debugging breeding behavior. These are intentionally decoupled from runtime records
/// to keep dumps diff-friendly across refactors.
/// </summary>
public static class BreedingDumpDtos
{
    public const int SchemaVersion = 1;

    public sealed record BreedingPlanDumpDto(
        [property: JsonPropertyName("plan_id")] string PlanId,
        [property: JsonPropertyName("iso_date")] string IsoDate,
        [property: JsonPropertyName("seed")] int Seed,
        [property: JsonPropertyName("scope_animal_keys")] IReadOnlyList<string> ScopeAnimalKeys,
        [property: JsonPropertyName("pairs")] IReadOnlyList<BreedingPairDumpDto> Pairs,
        [property: JsonPropertyName("mutations")] IReadOnlyList<string> Mutations,
        [property: JsonPropertyName("diagnostics")] IReadOnlyList<string> Diagnostics
    );

    public sealed record BreedingPairDumpDto(
        [property: JsonPropertyName("female_key")] string FemaleAnimalKey,
        [property: JsonPropertyName("male_key")] string MaleAnimalKey,
        [property: JsonPropertyName("other_male_keys")] IReadOnlyList<string> OtherMaleKeys,
        [property: JsonPropertyName("mode")] string Mode,
        [property: JsonPropertyName("egg_to_clear")] string? EggToClearItemId,
        [property: JsonPropertyName("egg_to_assign")] string? EggToAssignItemId,
        [property: JsonPropertyName("fertilized_succeeded")] bool FertilizedSucceeded,
        [property: JsonPropertyName("male_quantity")] int MaleQuantity,
        [property: JsonPropertyName("base_fertilized_chance")] double BaseFertilizedChance,
        [property: JsonPropertyName("adjusted_fertilized_chance")] double AdjustedFertilizedChance,
        [property: JsonPropertyName("male_chance")] double MaleChance,
        [property: JsonPropertyName("boost_by_male")] double BoostByMale,
        [property: JsonPropertyName("apply_daily_luck_boost")] bool ApplyDailyLuckBoost,
        [property: JsonPropertyName("selected_egg_type")] string? SelectedEggType
    );

    public sealed record CoopAdultCountsDumpDto(
        [property: JsonPropertyName("coop_index")] int CoopIndex,
        [property: JsonPropertyName("adult_counts")] IReadOnlyList<AnimalCountDumpDto> AdultCounts
    );

    public sealed record AnimalCountDumpDto(
        [property: JsonPropertyName("animal_key")] string AnimalKey,
        [property: JsonPropertyName("count")] int Count
    );

    public static IReadOnlyList<BreedingPlanDumpDto> FromPlans(IEnumerable<BreedingPlan> plans)
        => plans
            .OrderBy(p => p.PlanId, StringComparer.OrdinalIgnoreCase)
            .Select(FromPlan)
            .ToList();

    public static BreedingPlanDumpDto FromPlan(BreedingPlan plan)
    {
        if (plan is null) throw new ArgumentNullException(nameof(plan));

        var scope = (plan.ScopeAnimalKeys ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var pairs = (plan.Pairs ?? Array.Empty<BreedingPairPlan>())
            .Select(p => new BreedingPairDumpDto(
                FemaleAnimalKey: p.FemaleAnimalKey,
                MaleAnimalKey: p.MaleAnimalKey,
                OtherMaleKeys: (p.OtherMaleKeys ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
                Mode: p.Mode.ToString(),
                EggToClearItemId: p.EggToClearItemId,
                EggToAssignItemId: p.EggToAssignItemId,
                FertilizedSucceeded: p.FertilizedSucceeded,
                MaleQuantity: p.MaleQuantity,
                BaseFertilizedChance: p.BaseFertilizedChance,
                AdjustedFertilizedChance: p.AdjustedFertilizedChance,
                MaleChance: p.MaleChance,
                BoostByMale: p.BoostByMale,
                ApplyDailyLuckBoost: p.ApplyDailyLuckBoost,
                SelectedEggType: p.SelectedEggType
            ))
            .ToList();

        var mutations = (plan.Mutations ?? Array.Empty<BreedingMutation>())
            .Select(m => m.ToString() ?? string.Empty)
            .ToList();

        var diagnostics = (plan.Diagnostics ?? Array.Empty<string>()).ToList();

        return new BreedingPlanDumpDto(
            PlanId: plan.PlanId,
            IsoDate: plan.InGameDate.ToString("yyyy-MM-dd"),
            Seed: plan.Seed,
            ScopeAnimalKeys: scope,
            Pairs: pairs,
            Mutations: mutations,
            Diagnostics: diagnostics
        );
    }

    public static IReadOnlyList<CoopAdultCountsDumpDto> FromCoopIndex(CoopBreedingIndex index)
    {
        if (index is null) throw new ArgumentNullException(nameof(index));

        var raw = index.GetAdultCountsByCoop();

        var list = new List<CoopAdultCountsDumpDto>(raw.Count);
        for (var i = 0; i < raw.Count; i++)
        {
            var counts = raw[i]
                .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
                .Select(kvp => new AnimalCountDumpDto(kvp.Key, kvp.Value))
                .ToList();

            list.Add(new CoopAdultCountsDumpDto(i, counts));
        }

        return list;
    }
}
