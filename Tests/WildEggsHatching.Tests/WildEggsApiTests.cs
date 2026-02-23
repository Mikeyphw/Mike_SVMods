using System.Collections.Generic;
using WildEggsHatching.Api;
using WildEggsHatching.Application.Facade;
using WildEggsHatching.Application.AnimalNotes;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Api;
using WildEggsHatching.Infrastructure.Config;
using Xunit;
using WildEggsHatching.Infrastructure.ModData;

namespace WildEggsHatching.Tests;

public sealed class WildEggsApiTests
{
    [Fact]
    public void Api_IsQueryOnly_AndForwardsToFacade()
    {
        var facade = new FakeFacade
        {
            Step = ProgressionStep.PartII,
            Config = new ModConfig { ProgressionMode = true, DesertTombModuleEnabled = false },
            Days = new Dictionary<string, int> { { "q1", 5 } }
        };

        IWildEggsApi api = new WildEggsApi(facade, new FakeModDataLoader());

        Assert.Equal(1, api.ApiVersion);
        Assert.Equal(ProgressionStep.PartII, api.GetProgressionStep());
        Assert.True(api.IsProgressionModeEnabled());
        Assert.False(api.IsDesertTombModuleEnabled());

        Assert.NotNull(api.BirthTextMode);
        Assert.NotNull(api.ValidAnimals);
        api.SetBirthTextMode("Mysterious");
        Assert.Equal("Mysterious", api.BirthTextMode);
        api.SetChosenAnimal("Chicken", "Coop", isMagic: false);
        Assert.Equal("Chicken", api.ChosenAnimal_Coop);
        Assert.NotNull(api.WildEggsIDs);

        var days = api.GetQuestCompletionDays();
        Assert.Equal(5, days["q1"]);
    }

private sealed class FakeModDataLoader : IModDataLoader
{
    public ModDataSnapshot Load() => ModDataSnapshot.Empty;
}

    private sealed class FakeFacade : IModFacade
    {
        public ProgressionStep Step { get; set; }
        public ModConfig Config { get; set; } = new();
        public IReadOnlyDictionary<string, int> Days { get; set; } = new Dictionary<string, int>();

        public ProgressionStep GetProgressionStep() => Step;

        public ModConfig GetConfigSnapshot() => Config;

        public IReadOnlyDictionary<string, int> GetQuestCompletionDays() => Days;

        public FacadeResult ReloadAll() => new(true, "ok");
        public FacadeResult TrySetProgressionStep(ProgressionStep step, bool applySideEffects, string reason) => new(false, "no");
        public FacadeResult TryPlaceItem(string locationName, string qualifiedItemId, int count, int maxAttempts) => new(false, "no");
        public FacadeResult TryRunSpawnPlanNow() => new(false, "no");

        public FacadeResult DumpAnimalNotes() => new(false, "no");
        public FacadeResult DumpAnimalNotes(AnimalNotesDumpOptions options) => new(false, "no");
        public FacadeResult RepairAnimalNotes(bool requeueMail = true, bool reconcileNotes = true, bool force = false) => new(false, "no");
        public FacadeResult ValidateAnimalNotes() => new(false, "no");
        public FacadeResult WipeSchemaData(bool confirmed) => new(false, "no");
    }
}