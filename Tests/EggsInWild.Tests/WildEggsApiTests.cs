using EggsInWild.Api;
using EggsInWild.Core.Enums;
using EggsInWild.Infrastructure.Api;
using EggsInWild.Infrastructure.Config;
using Xunit;

namespace EggsInWild.Tests;

public sealed class WildEggsApiTests
{
    [Fact]
    public void Api_IsQueryOnly_AndForwardsToFacade()
    {
        var config = new ModConfig
        {
            ProgressionMode = true,
            DesertTombModuleEnabled = false,
        };

        IWildEggsApi api = new WildEggsApi(() => config);

        Assert.Equal(1, api.ApiVersion);
        Assert.Equal(ProgressionStep.PartI, api.GetProgressionStep());
        Assert.True(api.IsProgressionModeEnabled());
        Assert.False(api.IsDesertTombModuleEnabled());

        Assert.NotNull(api.BirthTextMode);
        Assert.NotNull(api.ValidAnimals);
        api.SetBirthTextMode("Mysterious");
        Assert.Equal("Mysterious", api.BirthTextMode);
        api.SetChosenAnimal("Chicken", "Coop", isMagic: false);
        Assert.Equal("Chicken", api.ChosenAnimal_Coop);
        Assert.NotNull(api.WildEggsIDs);

        Assert.Empty(api.GetQuestCompletionDays());
    }
}
