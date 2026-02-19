using System;
using System.Collections.Generic;
using LetMeHatchThisEgg.Infrastructure.GameData;
using Moq;
using StardewModdingAPI;
using StardewValley.GameData.FarmAnimals;
using Xunit;

namespace LetMeHatchThisEgg.Core.Tests;

public sealed class SmapiFarmAnimalAssetAdapterTests
{
    [Fact]
    public void Wrap_Null_Throws()
    {
        var adapter = new SmapiFarmAnimalAssetAdapter();
        Assert.Throws<ArgumentNullException>(() => adapter.Wrap(null!));
    }

    [Fact]
    public void Wrap_Returns_Repository_BackByAssetDictionary()
    {
        var backing = new Dictionary<string, FarmAnimalData>(StringComparer.Ordinal)
        {
            ["Chicken"] = new FarmAnimalData(),
        };

                var dictView = new Mock<IAssetDataForDictionary<string, FarmAnimalData>>(MockBehavior.Strict);
        dictView.SetupGet(d => d.Data).Returns(backing);

        var asset = new Mock<IAssetData>(MockBehavior.Strict);
        asset.Setup(a => a.AsDictionary<string, FarmAnimalData>()).Returns(dictView.Object);

        var adapter = new SmapiFarmAnimalAssetAdapter();
        var repo = adapter.Wrap(asset.Object);

        Assert.Same(backing, repo.Data);

        var duck = new FarmAnimalData();
        repo.Set("Duck", duck);

        Assert.Same(duck, backing["Duck"]);
        Assert.Same(backing["Chicken"], repo.Get("Chicken"));
    }
}
