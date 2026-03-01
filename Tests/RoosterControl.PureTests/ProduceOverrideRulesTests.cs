using System.Linq;
using RoosterControl.Application.Produces;
using Xunit;

namespace RoosterControl.PureTests;

public sealed class ProduceOverrideRulesTests
{
    [Fact]
    public void Parse_ReturnsValidEmpty_ForNullOrWhitespace()
    {
        var r1 = ProduceOverrideRules.Parse(null);
        var r2 = ProduceOverrideRules.Parse("");
        var r3 = ProduceOverrideRules.Parse("   ");

        Assert.True(r1.IsValid);
        Assert.Empty(r1.Issues);
        Assert.Empty(r1.RuleSet.Rules);

        Assert.True(r2.IsValid);
        Assert.Empty(r2.Issues);
        Assert.Empty(r2.RuleSet.Rules);

        Assert.True(r3.IsValid);
        Assert.Empty(r3.Issues);
        Assert.Empty(r3.RuleSet.Rules);
    }

    [Fact]
    public void Parse_SupportsRootRulesArray()
    {
        var json =
            @"[
  {
    ""id"": ""only-white"",
    ""when"": { ""roosterType"": ""White"", ""day"": 7, ""produceType"": ""produce"" },
    ""then"": { ""excludeKeys"": [""A"", ""B""] }
  }
]";

        var result = ProduceOverrideRules.Parse(json);

        Assert.True(result.IsValid);
        Assert.Single(result.RuleSet.Rules);

        var rule = result.RuleSet.Rules[0];
        Assert.Equal(0, rule.OrderIndex);
        Assert.Equal("only-white", rule.Id);
        Assert.Equal("White", rule.When.RoosterType);
        Assert.Equal(7, rule.When.Day);
        Assert.Equal(ProduceOverrideProduceType.Produce, rule.When.ProduceType);

        // ExcludeKeys are normalized + deterministic (sorted, unique).
        Assert.Equal(new[] { "A", "B" }, rule.Then.ExcludeKeys.ToArray());
    }

    [Fact]
    public void Parse_SupportsRootObjectWithRulesProperty()
    {
        var json =
            @"{
  ""rules"": [
    {
      ""when"": { ""produceType"": ""deluxe"" },
      ""then"": { ""hardOverrideKey"": ""X"" }
    }
  ]
}";

        var result = ProduceOverrideRules.Parse(json);

        Assert.True(result.IsValid);
        Assert.Single(result.RuleSet.Rules);
        Assert.Equal(ProduceOverrideProduceType.Deluxe, result.RuleSet.Rules[0].When.ProduceType);
        Assert.Equal("X", result.RuleSet.Rules[0].Then.HardOverrideKey);
    }

    [Fact]
    public void Parse_ReportsError_ForInvalidJson()
    {
        var result = ProduceOverrideRules.Parse("{");

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == "invalid_json");
    }

    [Fact]
    public void Parse_ReportsError_WhenThenIsMissing()
    {
        var json = @"[ { ""when"": { ""roosterType"": ""White"" } } ]";

        var result = ProduceOverrideRules.Parse(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == "missing_then");
    }

    [Fact]
    public void Parse_ReportsError_ForInvalidDayOrProduceType()
    {
        var json =
            @"[
  {
    ""when"": { ""day"": 29, ""produceType"": ""nope"" },
    ""then"": { ""excludeKeys"": [""A""] }
  }
]";

        var result = ProduceOverrideRules.Parse(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == "invalid_day");
        Assert.Contains(result.Issues, i => i.Code == "invalid_produce_type");
    }

    [Fact]
    public void Parse_ReportsWarning_ForUnknownFields()
    {
        var json =
            @"[
  {
    ""when"": { ""roosterType"": ""White"", ""unknown"": 1 },
    ""then"": { ""excludeKeys"": [""A""], ""mystery"": true }
  }
]";

        var result = ProduceOverrideRules.Parse(json);

        Assert.True(result.IsValid); // unknown fields are warnings
        Assert.Contains(result.Issues, i => i.Severity == ProduceOverrideIssueSeverity.Warning && i.Code == "unknown_field");
    }

    [Fact]
    public void Parse_ReportsError_ForInvalidMultiplier()
    {
        var json =
            @"[
  {
    ""then"": {
      ""weightMultipliers"": {
        ""A"": 0
      }
    }
  }
]";

        var result = ProduceOverrideRules.Parse(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == "invalid_multiplier");
    }
}
