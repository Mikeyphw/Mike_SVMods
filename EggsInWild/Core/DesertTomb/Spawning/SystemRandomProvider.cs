using System;

namespace EggsInWild.DesertTomb.Core.Spawning;

/// <summary>
/// Adapter for <see cref="Random"/> to <see cref="IRandomProvider"/>.
/// </summary>
public sealed class SystemRandomProvider : IRandomProvider
{
    private readonly Random _random;

    public SystemRandomProvider(Random random)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);

    public double NextDouble() => _random.NextDouble();
}
