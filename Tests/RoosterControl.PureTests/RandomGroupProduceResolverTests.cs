using System;
using System.Collections.Generic;
using RoosterControl.Application.Catalogs;
using RoosterControl.Application.Produces;
using RoosterControl.Constants;
using Xunit;

namespace RoosterControl.PureTests;

public sealed class RandomGroupProduceResolverTests
{
    private sealed class FixedRandom : Random
    {
        private readonly int _value;
        public FixedRandom(int value) => _value = value;
        public override int Next(int maxValue) => maxValue <= 0 ? 0 : Math.Abs(_value) % maxValue;
    }

    [Fact]
    public void Resolve_ReturnsTranslatedItemId_FromRandomGroup()
    {
        var catalog = new ProduceCatalog
        {
            RandomGroups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Random Gems"] = new() { ItemIds.Gemstones.Amethyst, ItemIds.Gemstones.Jade }
            }
        };

        var resolver = new RandomGroupProduceResolver(catalog, new FixedRandom(0));

        var itemId = resolver.Resolve(ItemIds.RandomProduces.RandomGems);

        Assert.Equal("66", itemId); // Amethyst
    }

    [Fact]
    public void Resolve_SupportsNestedGroups()
    {
        var catalog = new ProduceCatalog
        {
            RandomGroups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = new() { "B" },
                ["B"] = new() { ItemIds.Gemstones.Jade }
            }
        };

        var resolver = new RandomGroupProduceResolver(catalog, new FixedRandom(0));

        var itemId = resolver.Resolve("A");

        Assert.Equal("70", itemId); // Jade
    }

    [Fact]
    public void Resolve_DoesNotInfiniteLoop_OnCycle()
    {
        var catalog = new ProduceCatalog
        {
            RandomGroups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = new() { "B" },
                ["B"] = new() { "A" }
            }
        };

        var resolver = new RandomGroupProduceResolver(catalog, new FixedRandom(0));

        var value = resolver.Resolve("A");

        Assert.Equal("A", value); // cycle breaks by returning untranslated key
    }

    [Fact]
    public void ResolveDeterministic_IsStable_ForSameSeed()
    {
        var catalog = new ProduceCatalog
        {
            RandomGroups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Random Gems"] = new() { ItemIds.Gemstones.Amethyst, ItemIds.Gemstones.Jade }
            }
        };

        var resolver = new RandomGroupProduceResolver(catalog);

        var first = resolver.Resolve(ItemIds.RandomProduces.RandomGems, baseSeed: 12345);
        var second = resolver.Resolve(ItemIds.RandomProduces.RandomGems, baseSeed: 12345);

        Assert.Equal(first, second);
    }

    [Fact]
    public void ResolveDeterministic_StableAcrossCallOrder()
    {
        var catalog = new ProduceCatalog
        {
            RandomGroups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = new() { "B", "C" },
                ["B"] = new() { ItemIds.Gemstones.Jade },
                ["C"] = new() { ItemIds.Gemstones.Amethyst },
            }
        };

        var resolver = new RandomGroupProduceResolver(catalog);

        var a1 = resolver.Resolve("A", baseSeed: 999);
        var b1 = resolver.Resolve("B", baseSeed: 999);

        var b2 = resolver.Resolve("B", baseSeed: 999);
        var a2 = resolver.Resolve("A", baseSeed: 999);

        Assert.Equal(a1, a2);
        Assert.Equal(b1, b2);
    }

    [Fact]
    public void ResolveWithTraceCached_ReportsCacheHit_OnSecondCall()
    {
        var catalog = new ProduceCatalog
        {
            RandomGroups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Random Gems"] = new() { ItemIds.Gemstones.Amethyst, ItemIds.Gemstones.Jade }
            }
        };

        var resolver = new RandomGroupProduceResolver(catalog, resolveCacheCapacity: 8, traceCacheCapacity: 8);

        var first = resolver.ResolveWithTraceCached(ItemIds.RandomProduces.RandomGems, baseSeed: 123);
        var second = resolver.ResolveWithTraceCached(ItemIds.RandomProduces.RandomGems, baseSeed: 123);

        Assert.False(first.CacheHit);
        Assert.True(second.CacheHit);

        var stats = resolver.GetCacheStats();
        Assert.Equal(1, stats.TraceCacheEntries);
        Assert.True(stats.TraceCacheHits >= 1);
        Assert.True(stats.TraceCacheMisses >= 1);
    }

    [Fact]
    public void ResolveDeterministic_CachesAreBounded_AndEvictLeastRecentlyUsed()
    {
        var resolver = new RandomGroupProduceResolver(produceCatalog: null, resolveCacheCapacity: 2, traceCacheCapacity: 2);

        _ = resolver.Resolve("K1", baseSeed: 1);
        _ = resolver.Resolve("K2", baseSeed: 1);
        _ = resolver.Resolve("K3", baseSeed: 1);

        var after3 = resolver.GetCacheStats();
        Assert.Equal(2, after3.ResolveCacheEntries);
        Assert.Equal(2, after3.ResolveCacheCapacity);
        Assert.Equal(1, after3.ResolveCacheEvictions);

        _ = resolver.Resolve("K2", baseSeed: 1); // should be a cache hit
        _ = resolver.Resolve("K1", baseSeed: 1); // was evicted; should miss and cause another eviction

        var after5 = resolver.GetCacheStats();
        Assert.True(after5.ResolveCacheHits >= 1);
        Assert.True(after5.ResolveCacheMisses >= 4);
        Assert.Equal(2, after5.ResolveCacheEvictions);
    }

    [Fact]
    public void ClearCaches_ResetsCountsAndEvictions()
    {
        var resolver = new RandomGroupProduceResolver(produceCatalog: null, resolveCacheCapacity: 2, traceCacheCapacity: 2);

        _ = resolver.Resolve("K1", baseSeed: 1);
        _ = resolver.ResolveWithTraceCached("K1", baseSeed: 1);

        _ = resolver.Resolve("K2", baseSeed: 1);
        _ = resolver.Resolve("K3", baseSeed: 1); // evict

        var before = resolver.GetCacheStats();
        Assert.True(before.ResolveCacheEntries > 0);
        Assert.True(before.TraceCacheEntries > 0);
        Assert.True(before.ResolveCacheEvictions >= 1);

        resolver.ClearCaches();

        var after = resolver.GetCacheStats();
        Assert.Equal(0, after.ResolveCacheEntries);
        Assert.Equal(0, after.TraceCacheEntries);
        Assert.Equal(0, after.ResolveCacheHits);
        Assert.Equal(0, after.ResolveCacheMisses);
        Assert.Equal(0, after.ResolveCacheEvictions);
        Assert.Equal(0, after.TraceCacheHits);
        Assert.Equal(0, after.TraceCacheMisses);
        Assert.Equal(0, after.TraceCacheEvictions);
    }
}
