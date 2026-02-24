using System;
using System.Collections.Generic;
using LetMeHatch.Cloning;
using LetMeHatch.Core.Cloning;
using LetMeHatch.Infrastructure.Cloning;
using LetMeHatch.Infrastructure.GameData;
using LetMeHatch.Models;
using Moq;
using StardewModdingAPI;
using StardewValley.GameData.FarmAnimals;
using Xunit;

namespace LetMeHatch.Core.Tests;

public sealed class FarmAnimalCloneBatchApplierTests
{
    [Fact]
    public void Apply_CallsClonerThenMappingForEachClone()
    {
        var events = new List<string>();

        var farmAnimals = new Dictionary<string, FarmAnimalData>
        {
            ["Chicken"] = new FarmAnimalData(),
            ["Chicken_Male"] = new FarmAnimalData(),
            ["Duck"] = new FarmAnimalData(),
            ["Duck_Male"] = new FarmAnimalData(),
        };

        var repo = new InMemoryFarmAnimalDataRepository(farmAnimals);

        var clones = new List<ClonedAnimalInfo>
        {
            new()
            {
                ContentPackID = "Pack.A",
                FemaleKey = "Chicken",
                MaleKey = "Chicken_Male",
            },
            new()
            {
                ContentPackID = "Pack.B",
                FemaleKey = "Duck",
                MaleKey = "Duck_Male",
            },
        };

        var packA = new Mock<IContentPack>(MockBehavior.Strict).Object;
        var packB = new Mock<IContentPack>(MockBehavior.Strict).Object;

        var resolver = new DictionaryContentPackResolver(new Dictionary<string, IContentPack>
        {
            ["Pack.A"] = packA,
            ["Pack.B"] = packB,
        });

        var cloner = new RecordingAnimalCloner(events);
        var mapping = new RecordingCloneMappingService(events);

        var applier = new FarmAnimalCloneBatchApplier(cloner, mapping);

        applier.Apply(repo, clones, resolver);

        Assert.Equal(
            new[]
            {
                "clone:Chicken_Male",
                "map:Chicken_Male",
                "clone:Duck_Male",
                "map:Duck_Male",
            },
            events.ToArray()
        );

        Assert.Equal(2, cloner.Calls.Count);
        Assert.Equal(("Pack.A", "Chicken", "Chicken_Male"), cloner.Calls[0]);
        Assert.Equal(("Pack.B", "Duck", "Duck_Male"), cloner.Calls[1]);

        Assert.Equal(2, mapping.Calls.Count);
        Assert.Same(farmAnimals, mapping.Calls[0].FarmAnimals);
        Assert.Same(farmAnimals, mapping.Calls[1].FarmAnimals);
    }

    private sealed class InMemoryFarmAnimalDataRepository : IFarmAnimalDataRepository
    {
        public InMemoryFarmAnimalDataRepository(IDictionary<string, FarmAnimalData> data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public IDictionary<string, FarmAnimalData> Data { get; }

        public FarmAnimalData Get(string key) => Data[key];

        public void Set(string key, FarmAnimalData value) => Data[key] = value;
    }

    private sealed class DictionaryContentPackResolver : IContentPackResolver
    {
        private readonly IReadOnlyDictionary<string, IContentPack> _packs;

        public DictionaryContentPackResolver(IReadOnlyDictionary<string, IContentPack> packs)
        {
            _packs = packs ?? throw new ArgumentNullException(nameof(packs));
        }

        public IContentPack GetById(string uniqueId) => _packs[uniqueId];
    }

    private sealed class RecordingAnimalCloner : IAnimalCloner
    {
        private readonly List<string> _events;

        public RecordingAnimalCloner(List<string> events)
        {
            _events = events;
        }

        public List<(string ContentPackId, string FemaleKey, string MaleKey)> Calls { get; } = new();

        public void EditClonedAnimal(
            IContentPack contentPack,
            ClonedAnimalInfo cloneInfo,
            FarmAnimalData originalAnimalData,
            FarmAnimalData currentCloneData
        )
        {
            Calls.Add((cloneInfo.ContentPackID, cloneInfo.FemaleKey, cloneInfo.MaleKey));
            _events.Add($"clone:{cloneInfo.MaleKey}");
        }
    }

    private sealed class RecordingCloneMappingService : ICloneMappingService
    {
        private readonly List<string> _events;

        public RecordingCloneMappingService(List<string> events)
        {
            _events = events;
        }

        public List<(ClonedAnimalInfo CloneInfo, IDictionary<string, FarmAnimalData> FarmAnimals)> Calls { get; } = new();

        public void ApplyAlternatePurchaseRules(
            ClonedAnimalInfo clonedAnimalInfo,
            IDictionary<string, FarmAnimalData> farmAnimalsData
        )
        {
            Calls.Add((clonedAnimalInfo, farmAnimalsData));
            _events.Add($"map:{clonedAnimalInfo.MaleKey}");
        }
    }
}
