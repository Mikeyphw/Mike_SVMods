using System;
using System.Collections.Generic;
using System.Linq;
using LetMeHatch.Config;
using LetMeHatch.Models;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Framework.Random;
using StardewValley.GameData.FarmAnimals;
using static LetMeHatch.EnumsAndStrings.Enums;

namespace LetMeHatch.Core.Breeding;

/// <summary>
/// Default planner that reproduces the existing LetMeHatch breeding selection and RNG behavior.
/// </summary>
public sealed class DefaultBreedingPlanner : IBreedingPlanner
{
    public BreedingPlan BuildPlan(BreedingPlanInput input)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));

        var diagnostics = new List<string>();
        var pairs = new List<BreedingPairPlan>();
        var mutations = new List<BreedingMutation>();

        var rng = input.Rng ?? throw new ArgumentNullException(nameof(input.Rng));
        var selector = new EggSelectionService(rng);

        var candidates = input.Candidates ?? Array.Empty<(string ContentPackId, BreedingDataFemale BreedingData)>();
        var viable = candidates.Where(x => x.BreedingData?.Males?.Any() == true).ToList();
        if (viable.Count == 0)
        {
            diagnostics.Add($"No viable breeding candidates for female '{input.FemaleKey}'.");
            return new BreedingPlan(
                PlanId: BuildPlanId(input),
                InGameDate: input.InGameDate,
                Seed: input.Seed,
                Pairs: pairs,
                Mutations: mutations,
                Diagnostics: diagnostics
            );
        }

        var chosenFemale = viable.GetRandomItem(_ => 1, randomProvider: rng);
        if (string.IsNullOrEmpty(chosenFemale.ContentPackId) || chosenFemale.BreedingData is null)
        {
            diagnostics.Add($"Skipped female '{input.FemaleKey}' because no chosen breeding data was available.");
            return new BreedingPlan(BuildPlanId(input), input.InGameDate, input.Seed, pairs, mutations, diagnostics);
        }

        var males = chosenFemale.BreedingData.Males ?? new Dictionary<string, BreedingDataMale>();
        var chosenMale = males
            .Where(x => x.Value is not null && x.Value.GetQuantity() > 0)
            .GetRandomItem(m => m.Value.GetQuantity(), randomProvider: rng);

        if (string.IsNullOrEmpty(chosenMale.Key) || chosenMale.Value is null)
        {
            diagnostics.Add($"Skipped female '{input.FemaleKey}' because no male breeding data was available.");
            return new BreedingPlan(BuildPlanId(input), input.InGameDate, input.Seed, pairs, mutations, diagnostics);
        }

        if (!TryGetGeneralConfig(input.BreedingConfigsByPack, chosenFemale.ContentPackId, input.FemaleKey, out var generalConfig))
        {
            diagnostics.Add($"Skipped female '{input.FemaleKey}' because no BreedingConfig was found for contentPack='{chosenFemale.ContentPackId}'.");
            return new BreedingPlan(BuildPlanId(input), input.InGameDate, input.Seed, pairs, mutations, diagnostics);
        }

        var eggs = GetEggs(generalConfig, chosenMale.Value);
        var maleKey = chosenMale.Key;
        var otherMales = males.Keys.Where(k => !string.Equals(k, maleKey, StringComparison.Ordinal)).ToList();

        var applyDailyLuckBoost = generalConfig.ApplyDailyLuckBoost;
        var maleChance = generalConfig.MaleChance.TryGetValue(maleKey, out var mc) ? mc : 0;
        var boostByMale = generalConfig.BoostByMale.TryGetValue(maleKey, out var bm) ? bm : 0;

        diagnostics.Add(
            $"LetMeHatch breeding pair selected: female='{input.FemaleKey}' male='{maleKey}' maleQty={chosenMale.Value.GetQuantity()} onlyFertilized={input.OnlyFertilizedEggs} maleChance={maleChance:0.###} boostByMale={boostByMale:0.###}."
        );

        if (input.OnlyFertilizedEggs)
        {
            BuildFertilizedPlan(
                input,
                selector,
                maleKey,
                otherMales,
                generalConfig,
                eggs,
                chosenMale.Value.GetQuantity(),
                applyDailyLuckBoost,
                maleChance,
                boostByMale,
                diagnostics,
                pairs
            );
        }
        else
        {
            BuildNormalPlan(
                input,
                selector,
                maleKey,
                otherMales,
                eggs,
                applyDailyLuckBoost,
                diagnostics,
                pairs
            );
        }

        mutations.Add(new BreedingMutation.EnforceMaleNeverLays(maleKey));
        foreach (var other in otherMales)
        {
            mutations.Add(new BreedingMutation.EnforceMaleNeverLays(other));
        }

        return new BreedingPlan(BuildPlanId(input), input.InGameDate, input.Seed, pairs, mutations, diagnostics);
    }

    private static void BuildNormalPlan(
        BreedingPlanInput input,
        EggSelectionService selector,
        string maleKey,
        IReadOnlyList<string> otherMales,
        EggSet eggs,
        bool applyDailyLuckBoost,
        List<string> diagnostics,
        List<BreedingPairPlan> pairs
    )
    {
        if (string.IsNullOrEmpty(eggs.NormalEggId) && string.IsNullOrEmpty(eggs.LargeEggId))
        {
            diagnostics.Add(
                $"Can't run hatching system for couple ({input.FemaleKey},{maleKey}), because there's no eggs available."
            );
            return;
        }

        var weighted = new List<(ItemType item, double weight)>();

        if (!string.IsNullOrEmpty(eggs.NormalEggId))
        {
            var w = applyDailyLuckBoost
                ? DailyLuckHelper.AdjustChanceByDailyLuck(eggs.NormalChance, input.DailyLuckBonus)
                : eggs.NormalChance;
            if (w > 0)
            {
                weighted.Add((ItemType.Normal, w));
            }
        }
        if (!string.IsNullOrEmpty(eggs.LargeEggId))
        {
            var w = applyDailyLuckBoost
                ? DailyLuckHelper.AdjustChanceByDailyLuck(eggs.LargeChance, input.DailyLuckBonus)
                : eggs.LargeChance;
            if (w > 0)
            {
                weighted.Add((ItemType.Large, w));
            }
        }

        if (weighted.Count == 0)
        {
            return;
        }

        var chosenType = selector.RollWeightedOrDefault(weighted, weighted[0].item);
        var chosenEggId = chosenType == ItemType.Large ? eggs.LargeEggId : eggs.NormalEggId;

        diagnostics.Add($"LetMeHatch normal roll: female='{input.FemaleKey}' male='{maleKey}' chosenType={chosenType}.");

        pairs.Add(
            new BreedingPairPlan(
                FemaleAnimalKey: input.FemaleKey,
                MaleAnimalKey: maleKey,
                OtherMaleKeys: otherMales,
                Mode: BreedingMode.Normal,
                EggToClearItemId: chosenEggId,
                EggToAssignItemId: chosenEggId,
                FertilizedSucceeded: false,
                MaleQuantity: 0,
                BaseFertilizedChance: 0,
                AdjustedFertilizedChance: 0,
                MaleChance: 0,
                BoostByMale: 0,
                ApplyDailyLuckBoost: applyDailyLuckBoost,
                SelectedEggType: chosenType.ToString()
            )
        );
    }

    private static void BuildFertilizedPlan(
        BreedingPlanInput input,
        EggSelectionService selector,
        string maleKey,
        IReadOnlyList<string> otherMales,
        BreedingConfig generalConfig,
        EggSet eggs,
        int maleQty,
        bool applyDailyLuckBoost,
        double maleChance,
        double boostByMale,
        List<string> diagnostics,
        List<BreedingPairPlan> pairs
    )
    {
        if (string.IsNullOrEmpty(eggs.FertilizedEggId))
        {
            diagnostics.Add(
                $"Can't run hatching system because no fertilized eggs are available. Couple: ({input.FemaleKey},{maleKey})."
            );
            return;
        }

        var baseChance = eggs.FertilizedChance + maleQty * boostByMale;
        var adjustedChance = applyDailyLuckBoost
            ? DailyLuckHelper.AdjustChanceByDailyLuck(baseChance, input.DailyLuckBonus)
            : baseChance;

        var fertilizedSucceeded = maleQty > 0
            && selector.RollChance(adjustedChance)
            && selector.RollChanceInclusive(maleChance);

        diagnostics.Add(
            $"LetMeHatch fertilized roll: female='{input.FemaleKey}' male='{maleKey}' chance={adjustedChance:0.###} maleChance={maleChance:0.###} => fertilized={fertilizedSucceeded}."
        );

        pairs.Add(
            new BreedingPairPlan(
                FemaleAnimalKey: input.FemaleKey,
                MaleAnimalKey: maleKey,
                OtherMaleKeys: otherMales,
                Mode: BreedingMode.Fertilized,
                EggToClearItemId: eggs.FertilizedEggId,
                EggToAssignItemId: fertilizedSucceeded ? eggs.FertilizedEggId : null,
                FertilizedSucceeded: fertilizedSucceeded,
                MaleQuantity: maleQty,
                BaseFertilizedChance: baseChance,
                AdjustedFertilizedChance: adjustedChance,
                MaleChance: maleChance,
                BoostByMale: boostByMale,
                ApplyDailyLuckBoost: applyDailyLuckBoost,
                SelectedEggType: ItemType.FertilizedEgg.ToString()
            )
        );
    }

    private static bool TryGetGeneralConfig(
        IReadOnlyDictionary<string, Dictionary<string, BreedingConfig>> configsByPack,
        string contentPackId,
        string femaleKey,
        out BreedingConfig config
    )
    {
        config = null!;
        if (!configsByPack.TryGetValue(contentPackId, out var byAnimal))
        {
            return false;
        }

        return byAnimal.TryGetValue(femaleKey, out config!);
    }

    private static EggSet GetEggs(BreedingConfig generalConfig, BreedingDataMale maleData)
    {
        var eggIds = maleData.GetEggIDs();
        return new EggSet(
            NormalEggId: eggIds[ItemType.Normal],
            LargeEggId: eggIds[ItemType.Large],
            FertilizedEggId: eggIds[ItemType.FertilizedEgg],
            NormalChance: generalConfig.EggChances[ItemType.Normal],
            LargeChance: generalConfig.EggChances[ItemType.Large],
            FertilizedChance: generalConfig.EggChances[ItemType.FertilizedEgg]
        );
    }

    private static string BuildPlanId(BreedingPlanInput input)
        => $"{input.InGameDate:yyyyMMdd}-{input.FemaleKey}-{input.Seed}";

    private sealed record EggSet(
        string? NormalEggId,
        string? LargeEggId,
        string? FertilizedEggId,
        double NormalChance,
        double LargeChance,
        double FertilizedChance);
}
