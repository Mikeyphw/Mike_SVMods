using System;
using System.Collections.Generic;
using System.Linq;

namespace LetMeHatch.Core.Breeding;

/// <summary>
/// Precomputed per-coop adult animal counts to support coop-scoped breeding decisions.
/// </summary>
public sealed class CoopBreedingIndex
{
    private readonly IReadOnlyList<CoopSnapshot> _coops;
    private readonly Dictionary<string, List<int>> _coopsByAdultType;
    private readonly Dictionary<(string MaleKey, string FemaleKey), int> _cache;

    private CoopBreedingIndex(IReadOnlyList<CoopSnapshot> coops, Dictionary<string, List<int>> coopsByAdultType)
    {
        _coops = coops;
        _coopsByAdultType = coopsByAdultType;
        _cache = new Dictionary<(string, string), int>(new PairKeyComparer());
    }

    /// <summary>
    /// Builds a coop index from precomputed adult counts per coop. Intended for tests and diagnostics.
    /// </summary>
    public static CoopBreedingIndex FromAdultCounts(IReadOnlyList<IDictionary<string, int>> adultCountsByCoop)
    {
        if (adultCountsByCoop is null) throw new ArgumentNullException(nameof(adultCountsByCoop));

        var coops = new List<CoopSnapshot>(adultCountsByCoop.Count);
        for (var i = 0; i < adultCountsByCoop.Count; i++)
        {
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in adultCountsByCoop[i])
            {
                if (!string.IsNullOrWhiteSpace(kvp.Key) && kvp.Value > 0)
                {
                    dict[kvp.Key] = kvp.Value;
                }
            }

            coops.Add(new CoopSnapshot(dict));
        }

        var byType = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < coops.Count; i++)
        {
            foreach (var kvp in coops[i].AdultCounts)
            {
                if (!byType.TryGetValue(kvp.Key, out var list))
                {
                    list = new List<int>();
                    byType[kvp.Key] = list;
                }

                if (kvp.Value > 0)
                {
                    list.Add(i);
                }
            }
        }

        return new CoopBreedingIndex(coops, byType);
    }

    /// <summary>
    /// Returns a read-only snapshot of the indexed adult counts per coop (by coop index).
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, int>> GetAdultCountsByCoop()
        => _coops.Select(c => (IReadOnlyDictionary<string, int>)c.AdultCounts).ToList();


    /// <summary>
    /// Gets the number of adult males of <paramref name="maleTypeKey"/> in coops that contain at least one adult
    /// animal of <paramref name="femaleTypeKey"/>.
    /// </summary>
    public int GetMaleQuantityForFemale(string maleTypeKey, string femaleTypeKey)
    {
        if (string.IsNullOrWhiteSpace(maleTypeKey) || string.IsNullOrWhiteSpace(femaleTypeKey))
        {
            return 0;
        }

        var key = (maleTypeKey, femaleTypeKey);
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        if (!_coopsByAdultType.TryGetValue(femaleTypeKey, out var femaleCoops) || femaleCoops.Count == 0)
        {
            _cache[key] = 0;
            return 0;
        }

        var sum = 0;
        foreach (var coopIdx in femaleCoops)
        {
            if (coopIdx < 0 || coopIdx >= _coops.Count)
            {
                continue;
            }

            if (_coops[coopIdx].AdultCounts.TryGetValue(maleTypeKey, out var count))
            {
                sum += count;
            }
        }

        _cache[key] = sum;
        return sum;
    }

    private sealed record CoopSnapshot(Dictionary<string, int> AdultCounts);

    private sealed class PairKeyComparer : IEqualityComparer<(string MaleKey, string FemaleKey)>
    {
        public bool Equals((string MaleKey, string FemaleKey) x, (string MaleKey, string FemaleKey) y)
            => string.Equals(x.MaleKey, y.MaleKey, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.FemaleKey, y.FemaleKey, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string MaleKey, string FemaleKey) obj)
            => HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.MaleKey ?? string.Empty),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.FemaleKey ?? string.Empty)
            );
    }

}
