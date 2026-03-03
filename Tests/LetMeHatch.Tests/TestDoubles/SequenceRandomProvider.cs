using System;
using System.Collections.Generic;
using Mikew.NestCore.Framework.Random;

namespace LetMeHatch.Tests.TestDoubles;

public sealed class SequenceRandomProvider : IRandomProvider
{
    private readonly Queue<double> _doubles;
    private readonly Queue<int> _ints;
    private readonly Queue<bool> _bools;

    public SequenceRandomProvider(IEnumerable<double>? doubles = null, IEnumerable<int>? ints = null, IEnumerable<bool>? bools = null)
    {
        _doubles = new Queue<double>(doubles ?? Array.Empty<double>());
        _ints = new Queue<int>(ints ?? Array.Empty<int>());
        _bools = new Queue<bool>(bools ?? Array.Empty<bool>());
    }

    public SequenceRandomProvider(params double[] doubles)
        : this((IEnumerable<double>)doubles)
    {
    }

    public double NextDouble() => _doubles.Count > 0 ? _doubles.Dequeue() : 0.0;

    public int Next(int maxExclusive)
    {
        if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        var value = _ints.Count > 0 ? _ints.Dequeue() : 0;
        return Math.Abs(value) % maxExclusive;
    }

    public int Next(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        var span = maxExclusive - minInclusive;
        return minInclusive + Next(span);
    }

    public bool NextBool() => _bools.Count > 0 ? _bools.Dequeue() : NextDouble() >= 0.5;

    public T NextItem<T>(IList<T> collection)
    {
        if (collection is null) throw new ArgumentNullException(nameof(collection));
        if (collection.Count == 0) throw new ArgumentException("Collection must be non-empty.", nameof(collection));
        return collection[Next(collection.Count)];
    }
}
