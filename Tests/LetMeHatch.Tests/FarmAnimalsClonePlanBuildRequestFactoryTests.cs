using System.Collections.Generic;
using LetMeHatch.Application.Gateways;
using LetMeHatch.Application.Services;
using StardewValley.GameData.FarmAnimals;
using Xunit;

namespace LetMeHatch.Tests;

public sealed class FarmAnimalsClonePlanBuildRequestFactoryTests
{
    [Fact]
    public void Create_MapsProduceListsToCoreSnapshot()
    {
        var animals = new Dictionary<string, FarmAnimalData>
        {
            ["Chicken"] = new FarmAnimalData
            {
                ProduceItemIds = new List<FarmAnimalProduce> { new() { ItemId = "176", Id = "176" } },
                DeluxeProduceItemIds = new List<FarmAnimalProduce> { new() { ItemId = "174", Id = "174" } }
            }
        };

        var factory = new FarmAnimalsClonePlanBuildRequestFactory(new FakeGameDataGateway(animals));
        var request = factory.Create("Mikew.letmehatch");

        Assert.True(request.Animals.TryGetValue("Chicken", out var produce));
        Assert.Contains("176", produce.ProduceItemIds);
        Assert.Contains("174", produce.DeluxeProduceItemIds);
    }

    private sealed class FakeGameDataGateway : IGameDataGateway
    {
        private readonly Dictionary<string, FarmAnimalData> _animals;

        public FakeGameDataGateway(Dictionary<string, FarmAnimalData> animals) => _animals = animals;

        public T Load<T>(string assetName) where T : notnull
        {
            Assert.Equal("Data/FarmAnimals", assetName);
            return (T)(object)_animals;
        }
    }
}
