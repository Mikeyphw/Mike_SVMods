using System;
using System.Collections.Generic;
using WildEggsHatching.DesertTomb.Core.Configuration;
using WildEggsHatching.DesertTomb.Core.Model;
using WildEggsHatching.DesertTomb.Core.Spawning;
using Xunit;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.Refactor.Tests;

public sealed class DesertTombSpawnPlannerTests
{
    [Fact]
    public void BuildPlan_RespectsGlobalMaxPerType()
    {
        var layout = DefaultTombLayouts.ByLevel[MineLevel.Level14];
        var config = SpawnConfiguration.Default;

        // Force a very low global cap for Mummy to validate clamping.
        config.Global.MaxMonsterInTomb[TombMonsterType.Mummy] = 1;

        var rng = new SequenceRandomProvider(
            ints: new[] { 5, 0, 0, 0, 0, 0, 0, 0 }, // capacity rolls + weighted rolls
            doubles: new[] { 0.0, 0.0, 0.0 }
        );

        var planner = new TombSpawnPlanner(rng);
        var plan = planner.BuildPlan(layout, config, TombDifficulty.Moderate);

        Assert.True(plan.TotalsByMonsterType.TryGetValue(TombMonsterType.Mummy, out var totalMummies));
        Assert.Equal(1, totalMummies);
    }

    private sealed class SequenceRandomProvider : IRandomProvider
    {
        private readonly Queue<int> _ints;
        private readonly Queue<double> _doubles;

        public SequenceRandomProvider(IEnumerable<int> ints, IEnumerable<double> doubles)
        {
            _ints = new Queue<int>(ints);
            _doubles = new Queue<double>(doubles);
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            if (_ints.Count == 0)
                return minInclusive;

            var value = _ints.Dequeue();

            // Map sequence value into the requested range deterministically.
            var range = Math.Max(1, maxExclusive - minInclusive);
            return minInclusive + (Math.Abs(value) % range);
        }

        public double NextDouble() => _doubles.Count == 0 ? 0.0 : _doubles.Dequeue();
    }
}
