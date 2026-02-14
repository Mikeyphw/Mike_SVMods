using System;
using System.Collections.Generic;
using Mikew.BirdsOfAFeather.Framework.Random;

namespace Mikew.BirdsOfAFeather.Helpers;

public static class WeightedRandomExtensions
{
    public static T GetRandomItem<T>(
        this IEnumerable<T> collection,
        Func<T, float> weightSelector,
        T? fallbackItem = default,
        bool useInverseWeights = false,
        IRandomProvider? randomProvider = null
    )
    {
        if (collection is null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        if (weightSelector is null)
        {
            throw new ArgumentNullException(nameof(weightSelector));
        }

        var rng = randomProvider ?? new GameRandomProvider();
        var table = new WeightedTable<T>(rng, useInverseWeights);

        var hasAny = false;
        foreach (var item in collection)
        {
            hasAny = true;
            table.Add(item, weightSelector(item));
        }

        if (!hasAny)
        {
            return fallbackItem!;
        }

        return table.RollOrDefault(fallbackItem!);
    }

    public static T GetRandomItem<T>(
        this IEnumerable<(T item, float weight)> weightedCollection,
        T? fallbackItem = default,
        bool useInverseWeights = false,
        IRandomProvider? randomProvider = null
    )
    {
        if (weightedCollection is null)
        {
            throw new ArgumentNullException(nameof(weightedCollection));
        }

        var rng = randomProvider ?? new GameRandomProvider();
        var table = new WeightedTable<T>(rng, useInverseWeights);

        var hasAny = false;
        foreach (var (item, weight) in weightedCollection)
        {
            hasAny = true;
            table.Add(item, weight);
        }

        if (!hasAny)
        {
            return fallbackItem!;
        }

        return table.RollOrDefault(fallbackItem!);
    }
}
