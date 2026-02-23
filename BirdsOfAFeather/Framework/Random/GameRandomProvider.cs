using System;
using System.Collections.Generic;
using StardewValley;

namespace Mikew.BirdsOfAFeather.Framework.Random;

/// <summary>
/// Uses <c>Game1.random</c>.
/// </summary>
public sealed class GameRandomProvider : IRandomProvider
{
    public double NextDouble() => Game1.random.NextDouble();

    public int Next(int maxExclusive) => Game1.random.Next(maxExclusive);

    public int Next(int minInclusive, int maxExclusive) => Game1.random.Next(minInclusive, maxExclusive);

    public bool NextBool() => Game1.random.NextDouble() < 0.5;

    public T NextItem<T>(IList<T> collection)
    {
        if (collection is null || collection.Count == 0)
        {
            throw new ArgumentException("Collection cannot be null or empty.", nameof(collection));
        }

        return collection[Game1.random.Next(collection.Count)];
    }
}
