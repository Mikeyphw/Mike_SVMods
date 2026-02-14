using Mikew.BirdsOfAFeather.Framework.Random;
using WildEggsHatching.Application.Random;
using Xunit;

namespace WildEggsHatching.Refactor.Tests;

public sealed class WeightedPickerTests
{
    [Fact]
    public void Pick_Is_Deterministic_With_FakeRandom()
    {
        var rng = new FakeRandomProvider(nextDouble: 0.0);
        var picker = new WeightedPicker<string>(rng);

        picker.Add("A", 1.0);
        picker.Add("B", 10.0);

        // With NextDouble = 0.0, WeightedTable will select the first bucket.
        Assert.Equal("A", picker.Pick());
    }

    private sealed class FakeRandomProvider : IRandomProvider
    {
        private readonly double _nextDouble;

        public FakeRandomProvider(double nextDouble) => _nextDouble = nextDouble;

        public double NextDouble() => _nextDouble;
        public int Next(int maxExclusive) => 0;
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
        public bool NextBool() => false;
        public T NextItem<T>(System.Collections.Generic.IList<T> collection) => collection[0];
    }
}
