using System;

namespace WildEggsHatching.Core.Random;

/// <summary>
/// A seeded random number generator for deterministic spawn behavior.
/// This replaces direct Game1.random usage for testable, reproducible spawn decisions.
/// </summary>
public sealed class SeededRandom
{
    private readonly Random _random;
    private readonly int _seed;

    /// <summary>
    /// Creates a new seeded random with the specified seed.
    /// </summary>
    /// <param name="seed">The seed for reproducible randomness.</param>
    public SeededRandom(int seed)
    {
        _seed = seed;
        _random = new Random(seed);
    }

    /// <summary>
    /// Creates a seeded random from combined parameters.
    /// Useful for creating context-specific seeds.
    /// </summary>
    public static int CreateSeed(params object[] components)
    {
        var combined = string.Join("|", components);
        return combined.GetHashCode();
    }

    /// <summary>
    /// Gets the seed used to initialize this random.
    /// </summary>
    public int Seed => _seed;

    /// <summary>
    /// Returns a random double between 0.0 and 1.0.
    /// </summary>
    public double NextDouble()
    {
        return _random.NextDouble();
    }

    /// <summary>
    /// Returns a random integer between 0 (inclusive) and maxValue (exclusive).
    /// </summary>
    public int Next(int maxValue)
    {
        return _random.Next(maxValue);
    }

    /// <summary>
    /// Returns a random integer between minValue (inclusive) and maxValue (exclusive).
    /// </summary>
    public int Next(int minValue, int maxValue)
    {
        return _random.Next(minValue, maxValue);
    }

    /// <summary>
    /// Returns a random boolean.
    /// </summary>
    public bool NextBool()
    {
        return _random.Next(2) == 0;
    }

    /// <summary>
    /// Picks a random item from the given collection.
    /// </summary>
    public T Pick<T>(System.Collections.Generic.IList<T> collection)
    {
        if (collection == null || collection.Count == 0)
        {
            throw new ArgumentException("Collection cannot be null or empty");
        }
        var index = _random.Next(collection.Count);
        return collection[index];
    }

    /// <summary>
    /// Picks a random item from the given collection using weights.
    /// </summary>
    public T PickWeighted<T>(System.Collections.Generic.IList<T> collection, System.Collections.Generic.IList<double> weights)
    {
        if (collection == null || collection.Count == 0)
        {
            throw new ArgumentException("Collection cannot be null or empty");
        }

        if (weights == null || weights.Count != collection.Count)
        {
            throw new ArgumentException("Weights must match collection size");
        }

        var totalWeight = 0.0;
        for (int i = 0; i < weights.Length; i++)
        {
            totalWeight += weights[i];
        }

        var randomValue = _random.NextDouble() * totalWeight;
        var runningWeight = 0.0;

        for (int i = 0; i < collection.Count; i++)
        {
            runningWeight += weights[i];
            if (randomValue < runningWeight)
            {
                return collection[i];
            }
        }

        return collection[collection.Count - 1];
    }

    /// <summary>
    /// Shuffles the given list in place using Fisher-Yates algorithm.
    /// </summary>
    public void Shuffle<T>(System.Collections.Generic.IList<T> list)
    {
        int n = list.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}

