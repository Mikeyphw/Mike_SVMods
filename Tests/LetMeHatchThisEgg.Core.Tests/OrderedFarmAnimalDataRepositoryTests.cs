using System;
using System.Collections.Generic;
using System.Linq;
using LetMeHatchThisEgg.Infrastructure.GameData;
using StardewValley.GameData.FarmAnimals;
using Xunit;

namespace LetMeHatchThisEgg.Core.Tests;

public sealed class OrderedFarmAnimalDataRepositoryTests
{
    private sealed class FakeFarmAnimalDataRepository : IFarmAnimalDataRepository
    {
        public FakeFarmAnimalDataRepository(IDictionary<string, FarmAnimalData> data) =>
            Data = data ?? throw new ArgumentNullException(nameof(data));

        public IDictionary<string, FarmAnimalData> Data { get; }

        public FarmAnimalData Get(string key) => Data[key];

        public void Set(string key, FarmAnimalData value) => Data[key] = value;
    }

    [Fact]
    public void FinalizeEdits_SortAllKeysOrdinal_RebuildsInOrdinalOrder()
    {
        var data = new Dictionary<string, FarmAnimalData>(StringComparer.Ordinal)
        {
            ["b"] = new FarmAnimalData(),
            ["a"] = new FarmAnimalData(),
            ["c"] = new FarmAnimalData(),
        };

        var inner = new FakeFarmAnimalDataRepository(data);
        var repo = new OrderedFarmAnimalDataRepository(inner, FarmAnimalDataRepositoryOrderingMode.SortAllKeysOrdinal);

        repo.FinalizeEdits();

        Assert.Equal(new[] { "a", "b", "c" }, repo.Data.Keys.ToList());
    }

    [Fact]
    public void FinalizeEdits_PreserveOriginalThenSortNewOrdinal_PreservesOriginalAndSortsNew()
    {
        var b = new FarmAnimalData();
        var a = new FarmAnimalData();
        var c = new FarmAnimalData();
        var aa = new FarmAnimalData();

        var data = new Dictionary<string, FarmAnimalData>(StringComparer.Ordinal)
        {
            ["b"] = b,
            ["a"] = a,
        };

        var inner = new FakeFarmAnimalDataRepository(data);
        var repo = new OrderedFarmAnimalDataRepository(
            inner,
            FarmAnimalDataRepositoryOrderingMode.PreserveOriginalThenSortNewOrdinal
        );

        repo.Set("c", c);
        repo.Set("aa", aa);

        repo.FinalizeEdits();

        Assert.Equal(new[] { "b", "a", "aa", "c" }, repo.Data.Keys.ToList());
        Assert.Same(b, repo.Get("b"));
        Assert.Same(a, repo.Get("a"));
        Assert.Same(aa, repo.Get("aa"));
        Assert.Same(c, repo.Get("c"));
    }

    [Fact]
    public void FinalizeEdits_None_DoesNotRebuild()
    {
        var data = new Dictionary<string, FarmAnimalData>(StringComparer.Ordinal);
        data["b"] = new FarmAnimalData();
        data["a"] = new FarmAnimalData();

        var inner = new FakeFarmAnimalDataRepository(data);
        var repo = new OrderedFarmAnimalDataRepository(inner, FarmAnimalDataRepositoryOrderingMode.None);

        repo.Set("c", new FarmAnimalData());

        repo.FinalizeEdits();

        Assert.Equal(new[] { "b", "a", "c" }, repo.Data.Keys.ToList());
    }
}
