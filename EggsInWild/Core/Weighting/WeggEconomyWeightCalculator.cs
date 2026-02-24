using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;
using EggsInWild.Core.ShopAnchors;
using EggsInWild.Infrastructure.Config;

namespace EggsInWild.Core.Weighting;

/// <summary>
/// Computes per-animal selection weights from economic effective values (profitable = rarer).
/// </summary>
public sealed class WeggEconomyWeightCalculator
{
    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

    private readonly IReadOnlyDictionary<string, FarmAnimalData> _farmAnimals;
    private readonly ShopAnchorIndex _anchors;
    private readonly EffectiveValueCalculator _effectiveValue;
    private readonly Func<string, bool> _isUnlocked;

    public WeggEconomyWeightCalculator(
        IReadOnlyDictionary<string, FarmAnimalData> farmAnimals,
        ShopAnchorIndex anchors,
        IReadOnlyDictionary<string, ObjectData> objects,
        Func<string, bool>? isUnlocked = null,
        int? horizonDays = null,
        double? deluxeProbability = null)
    {
        _farmAnimals = farmAnimals ?? throw new ArgumentNullException(nameof(farmAnimals));
        _anchors = anchors ?? throw new ArgumentNullException(nameof(anchors));
        _effectiveValue = new EffectiveValueCalculator(farmAnimals, anchors, objects, horizonDays, deluxeProbability);
        _isUnlocked = isUnlocked ?? (_ => false);
    }

    public IReadOnlyDictionary<string, double> ComputeWeights(IEnumerable<string> candidateIds)
    {
        if (candidateIds is null)
            throw new ArgumentNullException(nameof(candidateIds));

        var ids = candidateIds
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(KeyComparer)
            .ToList();

        if (ids.Count == 0)
            return new Dictionary<string, double>(KeyComparer);

        var values = _effectiveValue.ComputeForCandidates(ids);

        var baseWeights = new Dictionary<string, double>(KeyComparer);
        var baseList = new List<double>(capacity: ids.Count);

        foreach (var id in ids)
        {
            var v = values[id];
            var w = WeightFromValue(v);
            baseWeights[id] = w;
            baseList.Add(w);
        }

        var medianW = MedianMath.MedianOrDefault(baseList, defaultValue: 1.0);
        var minW = medianW / Math.Max(1e-9, DebugKnobs.WeightCapLow);
        var maxW = medianW * Math.Max(1e-9, DebugKnobs.WeightCapHigh);

        var finalWeights = new Dictionary<string, double>(KeyComparer);
        foreach (var id in ids)
        {
            var w = baseWeights[id];
            w = Clamp(w, minW, maxW);

            if (ShouldApplyHiddenNoAnchorPenalty(id))
                w *= DebugKnobs.WeightHiddenNoAnchorPenalty;

            if (_isUnlocked(id))
                w *= DebugKnobs.WeightUnlockedFactor;

            finalWeights[id] = Math.Max(0.0, w);
        }

        return finalWeights;
    }

    private static double WeightFromValue(double effectiveValue)
    {
        var v = Math.Max(0.0, effectiveValue);
        var denom = Math.Pow(v + DebugKnobs.WeightK, DebugKnobs.WeightAlpha);
        if (!double.IsFinite(denom) || denom <= 0)
            return 0;

        return 1.0 / denom;
    }

    private bool ShouldApplyHiddenNoAnchorPenalty(string animalId)
    {
        if (!_farmAnimals.TryGetValue(animalId, out var animal) || animal is null)
            return false;

        return animal.PurchasePrice < 0 && !_anchors.HasAnchor(animalId);
    }

    private static double Clamp(double v, double min, double max)
    {
        if (min > max)
            (min, max) = (max, min);

        if (v < min)
            return min;

        if (v > max)
            return max;

        return v;
    }
}
