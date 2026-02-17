using System;
using RoosterAddon.Infrastructure.StateDumps;
using Xunit;

namespace RoosterAddon.Tests;

public sealed class StateDumpJsonTests
{
    [Fact]
    public void BuildFileName_Is_Deterministic()
    {
        var ts = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var name = StateDumpJson.BuildFileName("roosteraddon_state", ts);
        Assert.Equal("roosteraddon_state_20250102_030405.json", name);
    }

    [Fact]
    public void SerializeIndented_Produces_Indented_Json()
    {
        var json = StateDumpJson.SerializeIndented(new { A = 1, B = "x" });

        Assert.Contains("\n", json);
        Assert.Contains("\"A\": 1", json);
        Assert.Contains("\"B\": \"x\"", json);
    }
}
