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
}
