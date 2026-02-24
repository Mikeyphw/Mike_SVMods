using System.Collections.Generic;
using Microsoft.Xna.Framework;
using EggsInWild.Application.Spawning;
using EggsInWild.Core.Enums;
using EggsInWild.Infrastructure.Game.TileRules;
using Xunit;

namespace EggsInWild.Tests;

public sealed class TileRulesTests
{
    private sealed class FakeTiles : ITileQuery
    {
        private readonly Dictionary<(int X, int Y), string> _terrain = new();
        private readonly HashSet<(int X, int Y)> _water = new();
        private readonly HashSet<(int X, int Y)> _placeable = new();
        private readonly HashSet<(int X, int Y)> _passable = new();

        public void SetTerrain(int x, int y, string terrain) => _terrain[(x, y)] = terrain;
        public void SetWater(int x, int y, bool isWater)
        {
            if (isWater) _water.Add((x, y));
            else _water.Remove((x, y));
        }

        public void SetPlaceable(int x, int y, bool placeable)
        {
            if (placeable) _placeable.Add((x, y));
            else _placeable.Remove((x, y));
        }

        public void SetPassable(int x, int y, bool passable)
        {
            if (passable) _passable.Add((x, y));
            else _passable.Remove((x, y));
        }

        public string GetTerrainType(int x, int y) => _terrain.TryGetValue((x, y), out var v) ? v : "";
        public bool IsWaterTile(int x, int y) => _water.Contains((x, y));
        public bool IsTotallyClearAndPlaceable(Vector2 tile) => _placeable.Contains(((int)tile.X, (int)tile.Y));
        public bool IsTilePassable(Vector2 tile) => _passable.Contains(((int)tile.X, (int)tile.Y));
    }

    private static SpawnJob Job(
        IReadOnlyList<string>? includeTerrain = null,
        IReadOnlyList<string>? excludeTerrain = null,
        IReadOnlyList<Rectangle>? includeRects = null,
        IReadOnlyList<Rectangle>? excludeRects = null)
        => new(
            EggItemId: "Egg",
            EggKind: EggKind.Normal,
            PackId: "Vanilla",
            LocationType: "Forest",
            MapName: "Forest",
            SpawnChance: 1.0,
            BypassRandomChance: true,
            EggsToSpawn: 1,
            IncludeTerrainTypes: includeTerrain ?? new List<string>(),
            ExcludeTerrainTypes: excludeTerrain ?? new List<string>(),
            IncludeRects: includeRects ?? new List<Rectangle>(),
            ExcludeRects: excludeRects ?? new List<Rectangle>());

    [Fact]
    public void BoundsRule_RejectsOutOfBounds_First()
    {
        var tiles = new FakeTiles();
        var job = Job();
        var rules = new ITileRule[] { new BoundsRule(), new ExcludedRectsRule(), new TerrainRule(), new WaterRule(), new PlaceableRule() };

        var ok = AcceptAll(rules, tiles, job, mapWidth: 5, mapHeight: 5, tile: new Vector2(-1, 0), out var reason);

        Assert.False(ok);
        Assert.Equal(TileRejectionReason.OutOfBounds, reason);
    }

    [Fact]
    public void ExcludedRectsRule_RejectsInsideExcludedRect()
    {
        var tiles = new FakeTiles();
        var job = Job(excludeRects: new List<Rectangle> { new(2, 2, 2, 2) }); // covers (2,2) and (3,3) in XNA semantics

        var rule = new ExcludedRectsRule();
        var ok = rule.Accept(tiles, job, 10, 10, new Vector2(2, 2), out var reason);

        Assert.False(ok);
        Assert.Equal(TileRejectionReason.ExcludedRect, reason);
    }

    [Fact]
    public void TerrainRule_IncludeList_RequiresMatch_CaseInsensitive()
    {
        var tiles = new FakeTiles();
        tiles.SetTerrain(1, 1, "Dirt");

        var job = Job(includeTerrain: new List<string> { "grass" });
        var rule = new TerrainRule();

        var ok = rule.Accept(tiles, job, 10, 10, new Vector2(1, 1), out var reason);

        Assert.False(ok);
        Assert.Equal(TileRejectionReason.Terrain, reason);
    }

    [Fact]
    public void TerrainRule_ExcludeList_RejectsMatch_CaseInsensitive()
    {
        var tiles = new FakeTiles();
        tiles.SetTerrain(1, 1, "GRASS");

        var job = Job(excludeTerrain: new List<string> { "grass" });
        var rule = new TerrainRule();

        var ok = rule.Accept(tiles, job, 10, 10, new Vector2(1, 1), out var reason);

        Assert.False(ok);
        Assert.Equal(TileRejectionReason.Terrain, reason);
    }

    [Fact]
    public void WaterRule_RejectsWater()
    {
        var tiles = new FakeTiles();
        tiles.SetWater(3, 4, true);

        var job = Job();
        var rule = new WaterRule();

        var ok = rule.Accept(tiles, job, 10, 10, new Vector2(3, 4), out var reason);

        Assert.False(ok);
        Assert.Equal(TileRejectionReason.Water, reason);
    }

    [Fact]
    public void PlaceableRule_Classifies_NotPlaceable_AsOccupied_WhenPassable()
    {
        var tiles = new FakeTiles();
        tiles.SetPlaceable(2, 2, false);
        tiles.SetPassable(2, 2, true);

        var job = Job();
        var rule = new PlaceableRule();

        var ok = rule.Accept(tiles, job, 10, 10, new Vector2(2, 2), out var reason);

        Assert.False(ok);
        Assert.Equal(TileRejectionReason.Occupied, reason);
    }

    [Fact]
    public void PlaceableRule_Classifies_NotPlaceable_AsImpassable_WhenNotPassable()
    {
        var tiles = new FakeTiles();
        tiles.SetPlaceable(2, 2, false);
        tiles.SetPassable(2, 2, false);

        var job = Job();
        var rule = new PlaceableRule();

        var ok = rule.Accept(tiles, job, 10, 10, new Vector2(2, 2), out var reason);

        Assert.False(ok);
        Assert.Equal(TileRejectionReason.Impassable, reason);
    }

    [Fact]
    public void RuleChain_StopsAtFirstFailure()
    {
        var tiles = new FakeTiles();
        var job = Job();

        var called = 0;

        var rules = new ITileRule[]
        {
            new RejectAlwaysRule(TileRejectionReason.Terrain),
            new CountingAcceptRule(() => called++),
        };

        var ok = AcceptAll(rules, tiles, job, 10, 10, new Vector2(0, 0), out var reason);

        Assert.False(ok);
        Assert.Equal(TileRejectionReason.Terrain, reason);
        Assert.Equal(0, called);
    }

    private static bool AcceptAll(IReadOnlyList<ITileRule> rules, ITileQuery tiles, SpawnJob job, int mapWidth, int mapHeight, Vector2 tile, out TileRejectionReason reason)
    {
        for (var i = 0; i < rules.Count; i++)
        {
            if (!rules[i].Accept(tiles, job, mapWidth, mapHeight, tile, out reason))
                return false;
        }

        reason = TileRejectionReason.None;
        return true;
    }

    private sealed class RejectAlwaysRule : ITileRule
    {
        private readonly TileRejectionReason _reason;
        public RejectAlwaysRule(TileRejectionReason reason) => _reason = reason;

        public bool Accept(ITileQuery tiles, SpawnJob job, int mapWidth, int mapHeight, Vector2 tile, out TileRejectionReason reason)
        {
            reason = _reason;
            return false;
        }
    }

    private sealed class CountingAcceptRule : ITileRule
    {
        private readonly System.Action _onCall;
        public CountingAcceptRule(System.Action onCall) => _onCall = onCall;

        public bool Accept(ITileQuery tiles, SpawnJob job, int mapWidth, int mapHeight, Vector2 tile, out TileRejectionReason reason)
        {
            _onCall();
            reason = TileRejectionReason.None;
            return true;
        }
    }
}
