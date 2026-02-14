using System;
using System.Collections.Generic;

namespace Mikew.BirdsOfAFeather.Framework.Random;

/// <summary>
/// Wraps a <see cref="System.Random"/> instance.
/// </summary>
public sealed class SystemRandomProvider : IRandomProvider
{
    private readonly System.Random _random;

    public SystemRandomProvider(System.Random random)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public double NextDouble() => _random.NextDouble();

    public int Next(int maxExclusive) => _random.Next(maxExclusive);

    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);

    public bool NextBool() => _random.NextDouble() >= 0.5;

    public T NextItem<T>(IList<T> collection)
    {
        if (collection is null || collection.Count == 0)
        {
            throw new ArgumentException("Collection cannot be null or empty.", nameof(collection));
        }

        return collection[_random.Next(collection.Count)];
    }
}
