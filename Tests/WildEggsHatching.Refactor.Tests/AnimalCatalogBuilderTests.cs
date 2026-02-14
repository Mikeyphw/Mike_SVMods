using System.Collections.Generic;
using StardewModdingAPI;
using WildEggsHatching.Infrastructure.Logging;
using WildEggsHatching.Infrastructure.ModData;
using WildEggsHatching.Infrastructure.ModData.Models;
using WildEggsHatching.Infrastructure.Mods;
using Xunit;

namespace WildEggsHatching.Refactor.Tests;

public sealed class AnimalCatalogBuilderTests
{
    [Fact]
    public void Mod_Filter_Excludes_Unloaded_Mod_Groups()
    {
        var json = new Dictionary<string, WildAnimalJson>
        {
            ["Chicken"] = new WildAnimalJson
            {
                ExtraModAnimalKeys = new Dictionary<string, ExtraModAnimalKeysJson>
                {
                    ["Vanilla"] = new ExtraModAnimalKeysJson
                    {
                        FarmAnimalKeysSameWeight = new List<string> { "White Chicken" }
                    },
                    ["Some.Mod"] = new ExtraModAnimalKeysJson
                    {
                        FarmAnimalKeysSameWeight = new List<string> { "SomeMod Chicken" }
                    }
                }
            }
        };

        var builder = new AnimalCatalogBuilder(new FakeMods(isLoaded: false), new NullLog());
        var catalog = builder.Build(json);

        Assert.True(catalog.TryGet("Chicken", out var def));
        Assert.Contains("White Chicken", def.FarmAnimalIdsSameWeight);
        Assert.DoesNotContain("SomeMod Chicken", def.FarmAnimalIdsSameWeight);
    }

    private sealed class FakeMods : IModAvailability
    {
        private readonly bool _isLoaded;

        public FakeMods(bool isLoaded) => _isLoaded = isLoaded;

        public bool IsLoaded(string uniqueId) => uniqueId == "Vanilla" || _isLoaded;
    }

    private sealed class NullLog : ILog
    {
        public void Log(string message, LogLevel level = LogLevel.Debug) { }
    }
}
