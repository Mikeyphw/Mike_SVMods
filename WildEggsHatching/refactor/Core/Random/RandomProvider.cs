using System;
using System.Collections.Generic;

namespace WildEggsHatching.Core.Random;

/// <summary>
/// Default implementation of IRandomProvider using Game1.random.
/// This uses the game's random source for compatibility with existing behavior.
/// For deterministic/testing scenarios, inject a seeded implementation.
/// </summary>
public sealed class RandomProvider : IRandomProvider
{
    /// <inheritdoc />
    public double NextDouble()
    {
        return Game1.random.NextDouble();
    }

    /// <inheritdoc />
    public int Next(int max)
    {
        return Game1.random.Next(max);
    }

    /// <inheritdoc />
    public int Next(int min, int max)
    {
        return Game1.random.Next(min, max);
    }

    /// <inheritdoc />
    public bool NextBool()
    {
        return Game1.random.NextBool();
    }

    /// <inheritdoc />
    public T NextItem<T>(IList<T> collection)
    {
        if (collection == null || collection.Count == 0)
        {
            throw new ArgumentException("Collection cannot be null or empty", nameof(collection));
        }

        var index = Game1.random.Next(collection.Count);
        return collection[index];
    }
}

/// <summary>
/// A seeded random provider for deterministic behavior.
/// Use this for testing or when reproducible results are needed.
/// </summary>
public sealed class SeededRandomProvider : IRandomProvider
{
    private readonly Random _random;

    /// <summary>
    /// Creates a new seeded random provider.
    /// </summary>
    /// <param name="seed">The seed for the random number generator.</param>
    public SeededRandomProvider(int seed)
    {
        _random = new Random(seed);
    }

    /// <summary>
    /// Creates a new seeded random provider using a hash of the provided inputs.
    /// </summary>
    /// <param name="day">Game day for seed generation.</param>
    /// <param name="location">Location name for seed generation.</param>
    /// <param name="seedModifier">Additional modifier for seed variation.</param>
    public SeededRandomProvider(int day, string location, int seedModifier = 0)
    {
        var combined = $"{day}:{location}:{seedModifier}";
        var hash = combined.GetHashCode();
        _random = new Random(hash);
    }

    /// <inheritdoc />
    public double NextDouble()
    {
        return _random.NextDouble();
    }

    /// <inheritdoc />
    public int Next(int max)
    {
        return _random.Next(max);
    }

    /// <inheritdoc />
    public int Next(int min, int max)
    {
        return _random.Next(min, max);
    }

    /// <inheritdoc />
    public bool NextBool()
    {
        return _random.NextDouble() >= 0.5;
    }

    /// <inheritdoc />
    public T NextItem<T>(IList<T> collection)
    {
        if (collection == null || collection.Count == 0)
        {
            throw new ArgumentException("Collection cannot be null or empty", nameof(collection));
        }

        var index = _random.Next(collection.Count);
        return collection[index];
    }
}
