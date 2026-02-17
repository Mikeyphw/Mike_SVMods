namespace WildEggsHatching.DesertTomb.Core.Spawning;

/// <summary>
/// Small abstraction for deterministic unit tests and to avoid coupling Core logic to any specific RNG source.
/// </summary>
public interface IRandomProvider
{
    int Next(int minInclusive, int maxExclusive);
    double NextDouble();
}
