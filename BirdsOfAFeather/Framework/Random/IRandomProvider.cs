using System.Collections.Generic;

namespace Mikew.BirdsOfAFeather.Framework.Random;

/// <summary>
/// Abstraction for random number generation to allow deterministic testing.
/// </summary>
public interface IRandomProvider
{
    double NextDouble();
    int Next(int maxExclusive);
    int Next(int minInclusive, int maxExclusive);
    bool NextBool();
    T NextItem<T>(IList<T> collection);
}
