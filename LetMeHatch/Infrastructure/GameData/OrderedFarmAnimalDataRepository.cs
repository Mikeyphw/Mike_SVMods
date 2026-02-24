using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley.GameData.FarmAnimals;

namespace LetMeHatch.Infrastructure.GameData;

/// <summary>Repository decorator that can rebuild the underlying dictionary to stabilize iteration order.</summary>
public sealed class OrderedFarmAnimalDataRepository : IFarmAnimalDataRepository, IFinalizableFarmAnimalDataRepository
{
    private readonly IFarmAnimalDataRepository _inner;
    private readonly FarmAnimalDataRepositoryOrderingMode _orderingMode;
    private readonly List<string> _originalKeyOrder;

    public OrderedFarmAnimalDataRepository(
        IFarmAnimalDataRepository inner,
        FarmAnimalDataRepositoryOrderingMode orderingMode = FarmAnimalDataRepositoryOrderingMode.None
    )
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _orderingMode = orderingMode;
        _originalKeyOrder = inner.Data.Keys.ToList();
    }

    public IDictionary<string, FarmAnimalData> Data => _inner.Data;

    public FarmAnimalData Get(string key) => _inner.Get(key);

    public void Set(string key, FarmAnimalData value) => _inner.Set(key, value);

    public void FinalizeEdits()
    {
        if (_orderingMode == FarmAnimalDataRepositoryOrderingMode.None)
            return;

        IDictionary<string, FarmAnimalData> data = _inner.Data;

        IEnumerable<string> orderedKeys = _orderingMode switch
        {
            FarmAnimalDataRepositoryOrderingMode.SortAllKeysOrdinal => data.Keys.OrderBy(k => k, StringComparer.Ordinal),

            FarmAnimalDataRepositoryOrderingMode.PreserveOriginalThenSortNewOrdinal
                => BuildPreserveOriginalThenSortNewKeys(data.Keys),

            _ => data.Keys,
        };

        var snapshot = orderedKeys
            .Select(key => new KeyValuePair<string, FarmAnimalData>(key, data[key]))
            .ToList();

        data.Clear();

        foreach (var (key, value) in snapshot)
            data[key] = value;
    }

    private IEnumerable<string> BuildPreserveOriginalThenSortNewKeys(ICollection<string> currentKeys)
    {
        var remaining = new HashSet<string>(currentKeys, StringComparer.Ordinal);

        foreach (var key in _originalKeyOrder)
        {
            if (remaining.Remove(key))
                yield return key;
        }

        foreach (var key in remaining.OrderBy(k => k, StringComparer.Ordinal))
            yield return key;
    }
}
