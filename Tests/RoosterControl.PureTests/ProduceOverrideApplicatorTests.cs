using System.Linq;
using RoosterControl.Application.Produces;
using Xunit;

namespace RoosterControl.PureTests;

public sealed class ProduceOverrideApplicatorTests
{
    [Fact]
    public void Apply_NoRules_ReturnsUnchanged()
    {
        var rules = ProduceOverrideRules.Parse("[]").RuleSet;
        var ctx = new ProduceOverrideContext("White", "Coop", "Farm", "Hen", 7, ProduceOverrideProduceType.Produce);

        var candidates = new[]
        {
            new ProduceOverrideCandidate("A", 10),
            new ProduceOverrideCandidate("B", 20),
            new ProduceOverrideCandidate("C", 30),
            new ProduceOverrideCandidate("D", 40),
        };

        var r = ProduceOverrideApplicator.Apply(rules, ctx, candidates);

        Assert.Empty(r.AppliedRules);
        Assert.Empty(r.Issues);
        Assert.Equal(candidates, r.EffectiveCandidates.ToArray());
    }

    [Fact]
    public void Apply_ExcludeKeys_ZeroesWeights()
    {
        var json =
            @"[
  { ""when"": { ""roosterType"": ""White"" }, ""then"": { ""excludeKeys"": [""B"", ""D""] } }
]";
        var rules = ProduceOverrideRules.Parse(json).RuleSet;
        var ctx = new ProduceOverrideContext("WhiteRooster", "Coop", "Farm", "Hen", 1, ProduceOverrideProduceType.Produce);

        var candidates = new[]
        {
            new ProduceOverrideCandidate("A", 10),
            new ProduceOverrideCandidate("B", 20),
            new ProduceOverrideCandidate("C", 30),
            new ProduceOverrideCandidate("D", 40),
        };

        var r = ProduceOverrideApplicator.Apply(rules, ctx, candidates);

        Assert.Single(r.AppliedRules);
        Assert.Equal(0, r.EffectiveCandidates[1].Weight);
        Assert.Equal(0, r.EffectiveCandidates[3].Weight);
        Assert.Equal(10, r.EffectiveCandidates[0].Weight);
        Assert.Equal(30, r.EffectiveCandidates[2].Weight);
    }

    [Fact]
    public void Apply_ReplaceKeys_MergesDuplicatesDeterministically()
    {
        var json =
            @"[
  { ""then"": { ""replaceKeys"": { ""A"": ""X"", ""B"": ""X"" } } }
]";
        var rules = ProduceOverrideRules.Parse(json).RuleSet;
        var ctx = new ProduceOverrideContext("Any", "Coop", "Farm", "Hen", 1, ProduceOverrideProduceType.Produce);

        var candidates = new[]
        {
            new ProduceOverrideCandidate("A", 10),
            new ProduceOverrideCandidate("B", 20),
            new ProduceOverrideCandidate("C", 30),
            new ProduceOverrideCandidate("D", 40),
        };

        var r = ProduceOverrideApplicator.Apply(rules, ctx, candidates);

        // A->X and B->X, then merge into earliest slot (slot 0).
        Assert.Equal("X", r.EffectiveCandidates[0].Key);
        Assert.Equal(30, r.EffectiveCandidates[0].Weight);
        Assert.Equal(0, r.EffectiveCandidates[1].Weight);
        Assert.Equal("X", r.EffectiveCandidates[1].Key);
    }

    [Fact]
    public void Apply_WeightMultipliers_RoundsAndClamps()
    {
        var json =
            @"[
  { ""then"": { ""weightMultipliers"": { ""A"": 1.6, ""C"": 0.25 } } }
]";
        var rules = ProduceOverrideRules.Parse(json).RuleSet;
        var ctx = new ProduceOverrideContext("Any", "Coop", "Farm", "Hen", 1, ProduceOverrideProduceType.Produce);

        var candidates = new[]
        {
            new ProduceOverrideCandidate("A", 10),
            new ProduceOverrideCandidate("B", 20),
            new ProduceOverrideCandidate("C", 30),
            new ProduceOverrideCandidate("D", 40),
        };

        var r = ProduceOverrideApplicator.Apply(rules, ctx, candidates);

        // 10 * 1.6 = 16 (away from zero) ; 30 * 0.25 = 7.5 => 8
        Assert.Equal(16, r.EffectiveCandidates[0].Weight);
        Assert.Equal(8, r.EffectiveCandidates[2].Weight);
    }

    [Fact]
    public void Apply_HardOverride_ForcesSelectionAndZeroesOthers()
    {
        var json =
            @"[
  { ""then"": { ""hardOverrideKey"": ""C"" } }
]";
        var rules = ProduceOverrideRules.Parse(json).RuleSet;
        var ctx = new ProduceOverrideContext("Any", "Coop", "Farm", "Hen", 1, ProduceOverrideProduceType.Produce);

        var candidates = new[]
        {
            new ProduceOverrideCandidate("A", 10),
            new ProduceOverrideCandidate("B", 20),
            new ProduceOverrideCandidate("C", 30),
            new ProduceOverrideCandidate("D", 40),
        };

        var r = ProduceOverrideApplicator.Apply(rules, ctx, candidates);

        Assert.Equal(0, r.EffectiveCandidates[0].Weight);
        Assert.Equal(0, r.EffectiveCandidates[1].Weight);
        Assert.Equal(100, r.EffectiveCandidates[2].Weight);
        Assert.Equal(0, r.EffectiveCandidates[3].Weight);
    }

    [Fact]
    public void Apply_WhenDoesNotMatch_DoesNotApply()
    {
        var json =
            @"[
  { ""when"": { ""day"": 7, ""produceType"": ""deluxe"" }, ""then"": { ""excludeKeys"": [""A""] } }
]";
        var rules = ProduceOverrideRules.Parse(json).RuleSet;
        var ctx = new ProduceOverrideContext("Any", "Coop", "Farm", "Hen", 1, ProduceOverrideProduceType.Produce);

        var candidates = new[]
        {
            new ProduceOverrideCandidate("A", 10),
            new ProduceOverrideCandidate("B", 20),
            new ProduceOverrideCandidate("C", 30),
            new ProduceOverrideCandidate("D", 40),
        };

        var r = ProduceOverrideApplicator.Apply(rules, ctx, candidates);

        Assert.Empty(r.AppliedRules);
        Assert.Equal(10, r.EffectiveCandidates[0].Weight);
    }
}
