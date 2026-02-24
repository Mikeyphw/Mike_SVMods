using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StardewValley.GameData.FarmAnimals;

namespace EggsInWild.Core.ShopAnchors;

/// <summary>
/// Maps hidden variant farm-animal IDs to a shop listing "anchor" price derived from a visible parent entry
/// (see <c>AlternatePurchaseTypes</c> in <c>Data/FarmAnimals</c>).
/// </summary>
public sealed class ShopAnchorIndex
{
    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

    private readonly Dictionary<string, int> _variantToAnchorListingPrice;

    private ShopAnchorIndex(Dictionary<string, int> variantToAnchorListingPrice)
        => _variantToAnchorListingPrice = variantToAnchorListingPrice;

    public static ShopAnchorIndex Empty { get; } = new(new Dictionary<string, int>(KeyComparer));

    public int Count => _variantToAnchorListingPrice.Count;

    public bool HasAnchor(string animalId)
        => !string.IsNullOrWhiteSpace(animalId) && _variantToAnchorListingPrice.ContainsKey(animalId);

    public bool TryGetAnchorListingPrice(string animalId, out int listingPrice)
        => _variantToAnchorListingPrice.TryGetValue(animalId, out listingPrice);

    public IReadOnlyDictionary<string, int> AsReadOnly()
        => _variantToAnchorListingPrice;

    public IEnumerable<KeyValuePair<string, int>> Sample(int count)
        => _variantToAnchorListingPrice
            .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(0, count));

    /// <summary>
    /// Build the index from <c>Data/FarmAnimals</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For each parent entry with <c>PurchasePrice &gt; 0</c>, we treat <c>2 * PurchasePrice</c> as the shop listing price.
    /// Every <c>AlternatePurchaseTypes[*].AnimalIDs</c> entry is then mapped to the lowest observed listing price, capped to the
    /// variant's own listing price if it is directly purchasable.
    /// </para>
    /// </remarks>
    public static ShopAnchorIndex Build(
        IReadOnlyDictionary<string, FarmAnimalData> farmAnimals,
        Action<string>? warn = null)
    {
        if (farmAnimals is null || farmAnimals.Count == 0)
            return Empty;

        var map = new Dictionary<string, int>(KeyComparer);

        foreach (var (parentId, parent) in farmAnimals)
        {
            if (parent is null || parent.PurchasePrice <= 0)
                continue;

            var parentListingPrice = parent.PurchasePrice * 2;
            if (parentListingPrice <= 0)
                continue;

            try
            {
                foreach (var childId in EnumerateAlternateAnimalIds(parent))
                {
                    if (string.IsNullOrWhiteSpace(childId))
                        continue;

                    var candidate = parentListingPrice;

                    if (farmAnimals.TryGetValue(childId, out var child) && child is not null && child.PurchasePrice > 0)
                    {
                        var childListing = child.PurchasePrice * 2;
                        if (childListing > 0)
                            candidate = Math.Min(candidate, childListing);
                    }

                    if (map.TryGetValue(childId, out var existing))
                        map[childId] = Math.Min(existing, candidate);
                    else
                        map[childId] = candidate;
                }
            }
            catch (Exception ex)
            {
                warn?.Invoke($"ShopAnchorIndex: failed processing '{parentId}'. {ex.GetType().Name}: {ex.Message}");
            }
        }

        return map.Count == 0 ? Empty : new ShopAnchorIndex(map);
    }

    private static IEnumerable<string> EnumerateAlternateAnimalIds(FarmAnimalData parent)
    {
        var prop = parent.GetType().GetProperty("AlternatePurchaseTypes", BindingFlags.Instance | BindingFlags.Public);
        if (prop?.GetValue(parent) is not System.Collections.IEnumerable alternates)
            yield break;

        foreach (var alt in alternates)
        {
            if (alt is null)
                continue;

            var idsProp =
                alt.GetType().GetProperty("AnimalIds", BindingFlags.Instance | BindingFlags.Public)
                ?? alt.GetType().GetProperty("AnimalIDs", BindingFlags.Instance | BindingFlags.Public)
                ?? alt.GetType().GetProperty("AnimalIds", BindingFlags.Instance | BindingFlags.Public);

            if (idsProp?.GetValue(alt) is not System.Collections.IEnumerable ids)
                continue;

            foreach (var raw in ids)
            {
                if (raw is string s)
                    yield return s;
            }
        }
    }
}
