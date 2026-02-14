using System;
using System.Collections.Generic;
using Mikew.BirdsOfAFeather.Framework.Random;
using Xunit;

namespace BirdsOfAFeather.Tests;

public class WeightedTableTests
{
    private sealed class StubRandom : IRandomProvider
    {
        private readonly Queue<double> _doubles = new();

        public StubRandom(params double[] doubles)
        {
            foreach (var d in doubles)
            {
                _doubles.Enqueue(d);
            }
        }

        public double NextDouble() => _doubles.Count > 0 ? _doubles.Dequeue() : 0.0;
        public int Next(int maxExclusive) => 0;
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
        public bool NextBool() => false;
        public T NextItem<T>(IList<T> collection) => collection[0];
    }

    [Fact]
    public void Roll_Empty_Throws()
    {
        var table = new WeightedTable<int>(new SeededRandomProvider(1));
        Assert.Throws<InvalidOperationException>(() => table.Roll());
    }

    [Fact]
    public void Roll_ZeroWeights_FallsBackToFirst()
    {
        var table = new WeightedTable<string>(new StubRandom(0.9));
        table.Add("A", 0);
        table.Add("B", 0);
        Assert.Equal("A", table.Roll());
    }

    [Fact]
    public void Roll_InverseWeights_UsesInverseTotal_NotOriginalTotal()
    {
        // weights: A=1 (inv=1), B=100 (inv=0.01), inverse total=1.01.
        // If the implementation incorrectly uses original total (101), a mid roll can wrongly fall through to last item.
        var rng = new StubRandom(0.5); // roll=0.505 on inverse total; should pick A.
        var table = new WeightedTable<string>(rng, useInverseWeights: true);
        table.Add("A", 1);
        table.Add("B", 100);

        Assert.Equal("A", table.Roll());
    }
}
