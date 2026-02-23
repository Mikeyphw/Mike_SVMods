using System;
using System.Collections.Generic;
using WildEggsHatching.DesertTomb.Core.Model.Loot;
using WildEggsHatching.Infrastructure.DesertTomb.Loot;
using WildEggsHatching.Infrastructure.DesertTomb.Loot.Definitions;
using WildEggsHatching.Infrastructure.ModData;
using Xunit;

namespace WildEggsHatching.Tests;

public sealed class TombDropTableEngineTests
{
    [Fact]
    public void IndependentChance_RespectsMaxItems()
    {
        var table = new TombDropTableDefinition(
            DropTableId: "t",
            Mode: TombDropRollMode.IndependentChance,
            MaxItems: 4,
            LuckyBonusEnabled: false,
            Entries: new[]
            {
                TombDropEntry.ChanceEntry("A", 1),
                TombDropEntry.ChanceEntry("B", 1),
                TombDropEntry.ChanceEntry("C", 1),
                TombDropEntry.ChanceEntry("D", 1),
                TombDropEntry.ChanceEntry("E", 1),
            }
        );

        var engine = new TombDropTableEngine();
        var items = engine.RollToItemIds(table, new Random(123), TombLuckContext.Neutral);

        Assert.Equal(4, items.Count);
    }

    [Fact]
    public void WeightedWithoutReplacement_PicksDistinct()
    {
        var table = new TombDropTableDefinition(
            DropTableId: "w",
            Mode: TombDropRollMode.WeightedWithoutReplacement,
            MaxItems: 2,
            LuckyBonusEnabled: false,
            Entries: new[]
            {
                TombDropEntry.WeightEntry("A", 10),
                TombDropEntry.WeightEntry("B", 1),
                TombDropEntry.WeightEntry("C", 1),
            }
        );

        var engine = new TombDropTableEngine();
        var items = engine.RollToItemIds(table, new Random(7), TombLuckContext.Neutral);

        Assert.Equal(2, items.Count);
        Assert.Equal(2, new HashSet<string>(items).Count);
    }

    [Fact]
    public void LuckyBonus_SuperUnlucky_CanZeroOutDrops()
    {
        var table = new TombDropTableDefinition(
            DropTableId: "luck",
            Mode: TombDropRollMode.IndependentChance,
            MaxItems: 4,
            LuckyBonusEnabled: true,
            Entries: new[]
            {
                TombDropEntry.ChanceEntry("A", 1),
            }
        );

        var engine = new TombDropTableEngine();
        var items = engine.RollToItemIds(table, new Random(1), new TombLuckContext(DailyLuck: -0.2, TeamAverageDailyLuck: 0));

        Assert.Empty(items);
    }

    [Fact]
    public void DropTables_LoadsFromJsonReader()
    {
        var reader = new InMemoryJsonFileReader(new Dictionary<string, string>
        {
            ["assets/DesertTomb/DropTables.json"] = @"{
              ""MyTable"": {
                ""Mode"": ""IndependentChance"",
                ""MaxItems"": 3,
                ""LuckyBonusEnabled"": false,
                ""Entries"": [
                  { ""Item"": ""A"", ""Chance"": 1.0 },
                  { ""Item"": ""B"", ""Chance"": 0.0 }
                ]
              }
            }"
        });

        var log = new FakeLog();
        var tables = new TombDropTables(reader, log);

        Assert.True(tables.TryGet("MyTable", out var table));
        Assert.Equal(3, table.MaxItems);
        Assert.Equal(TombDropRollMode.IndependentChance, table.Mode);
        Assert.Equal(2, table.Entries.Count); // B is chance 0; loader should keep but engine ignores; loader doesn't filter by chance, so we expect 2. Let's assert 2? Hmm.
    }
}

/// <summary>
/// Minimal JSON reader for unit tests (deserializes using System.Text.Json).
/// </summary>
internal sealed class InMemoryJsonFileReader : IJsonFileReader
{
    private readonly Dictionary<string, string> _jsonByPath;

    public InMemoryJsonFileReader(Dictionary<string, string> jsonByPath)
        => _jsonByPath = jsonByPath;

    public T? ReadJsonFile<T>(string relativePath) where T : class
    {
        if (!_jsonByPath.TryGetValue(relativePath, out var json))
            return null;

        return System.Text.Json.JsonSerializer.Deserialize<T>(
            json,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            }
        );
    }

    public string? ReadText(string relativePath)
        => _jsonByPath.TryGetValue(relativePath, out var json) ? json : null;
}
