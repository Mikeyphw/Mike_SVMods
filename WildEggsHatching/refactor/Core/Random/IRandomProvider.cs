using System;

namespace WildEggsHatching.Core.Random;

/// <summary>
/// Provides random number generation for the domain layer.
/// This abstraction allows seeded RNG for deterministic behavior and testing.
/// </summary>
public interface IRandomProvider
{
    /// <summary>
    /// Returns a random double between 0.0 and 1.0.
    /// </summary>
    double NextDouble();

    /// <summary>
    /// Returns a random integer between 0 (inclusive) and max (exclusive).
    /// </summary>
    int Next(int max);

    /// <summary>
    /// Returns a random integer between min (inclusive) and max (exclusive).
    /// </summary>
    int Next(int min, int max);

    /// <summary>
    /// Returns a random boolean.
    /// </summary>
    bool NextBool();

    /// <summary>
    /// Returns a random item from the given collection.
    /// </summary>
    T NextItem<T>(System.Collections.Generic.IList<T> collection);
}
