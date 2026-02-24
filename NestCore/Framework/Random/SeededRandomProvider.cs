using System;
using System.Collections.Generic;

namespace Mikew.NestCore.Framework.Random;

/// <summary>
/// Deterministic RNG for tests and reproducible behavior.
/// </summary>
public sealed class SeededRandomProvider : IRandomProvider
{
    private readonly System.Random _random;

    public SeededRandomProvider(int seed)
    {
        _random = new System.Random(seed);
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
