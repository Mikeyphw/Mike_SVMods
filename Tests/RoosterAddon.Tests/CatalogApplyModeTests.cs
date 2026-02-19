using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using RoosterAddon.Application.Catalogs;
using RoosterAddon.Enums;
using RoosterAddon.Framework.Config;
using StardewModdingAPI;
using Xunit;

namespace RoosterAddon.Tests;

public sealed class CatalogApplyModeTests
{
    private static (CatalogManager Manager, CatalogBundle Catalogs, IMonitor Monitor) Create()
    {
        var manager = new CatalogManager();
        var catalogs = CatalogDefaults.CreateDefaults();
        var monitor = Substitute.For<IMonitor>();
        return (manager, catalogs, monitor);
    }

    [Fact]
    public void ApplyToConfig_SeedOnly_DoesNotModifyExistingConfig()
    {
        var (manager, catalogs, monitor) = Create();
        var config = RoosterAddonConfig.CreateDefault();

        config.General.EggProbability = -1;
        config.General.BoostByRooster = 5000f;

        config.Roosters[RoosterType.White].Price = -10;
        config.Roosters[RoosterType.White].ProduceEntries = new List<ProduceEntry>();

        var changed = manager.ApplyToConfig(config, catalogs, isNewConfig: false, CatalogApplyMode.SeedOnly, monitor);

        Assert.False(changed);
        Assert.Equal(-1, config.General.EggProbability);
        Assert.Equal(5000f, config.General.BoostByRooster);
        Assert.Equal(-10, config.Roosters[RoosterType.White].Price);
        Assert.Empty(config.Roosters[RoosterType.White].ProduceEntries);
    }

    [Fact]
    public void ApplyToConfig_RepairInvalid_ClampsAndRepairsOnlyInvalid()
    {
        var (manager, catalogs, monitor) = Create();
        var config = RoosterAddonConfig.CreateDefault();

        config.General.EggProbability = -1;
        config.General.BoostByRooster = 5000f;

        config.Roosters[RoosterType.White].Price = -10;
        config.Roosters[RoosterType.White].ProduceEntries = new List<ProduceEntry>(); // invalid with CustomizeBelow

        config.Roosters[RoosterType.Brown].ProduceEntries = new List<ProduceEntry>
        {
            new() { Choice = "999", Probability = 100 }
        };

        config.Roosters[RoosterType.Blue].BlueEggChoice = string.Empty;

        var changed = manager.ApplyToConfig(config, catalogs, isNewConfig: false, CatalogApplyMode.RepairInvalid, monitor);

        Assert.True(changed);

        Assert.Equal(0, config.General.EggProbability);
        Assert.Equal(1000f, config.General.BoostByRooster);
        Assert.Equal(0, config.Roosters[RoosterType.White].Price);

        var expectedWhite = catalogs.Produce.Roosters["White"].Produce;
        AssertEntriesMatch(config.Roosters[RoosterType.White].ProduceEntries, expectedWhite);

        Assert.Single(config.Roosters[RoosterType.Brown].ProduceEntries);
        Assert.Equal("999", config.Roosters[RoosterType.Brown].ProduceEntries[0].Choice);

        Assert.Equal(catalogs.Produce.Roosters["Blue"].BlueEggChoice, config.Roosters[RoosterType.Blue].BlueEggChoice);
    }

    [Fact]
    public void ApplyToConfig_MergeMissing_FillsMissingWithoutOverwriting()
    {
        var (manager, catalogs, monitor) = Create();
        var config = RoosterAddonConfig.CreateDefault();

        config.Roosters.Remove(RoosterType.Blue);

        config.Roosters[RoosterType.White].ProduceEntries = new List<ProduceEntry>();

        var userEntry = new ProduceEntry { Choice = "999", Probability = 100 };
        config.Roosters[RoosterType.Brown].ProduceEntries = new List<ProduceEntry> { userEntry };

        var changed = manager.ApplyToConfig(config, catalogs, isNewConfig: false, CatalogApplyMode.MergeMissing, monitor);

        Assert.True(changed);
        Assert.True(config.Roosters.ContainsKey(RoosterType.Blue));

        AssertEntriesMatch(config.Roosters[RoosterType.White].ProduceEntries, catalogs.Produce.Roosters["White"].Produce);

        Assert.Single(config.Roosters[RoosterType.Brown].ProduceEntries);
        Assert.Equal("999", config.Roosters[RoosterType.Brown].ProduceEntries[0].Choice);
    }

    [Fact]
    public void ApplyToConfig_Authoritative_OverwritesConfigFromCatalogs()
    {
        var (manager, catalogs, monitor) = Create();
        var config = RoosterAddonConfig.CreateDefault();

        config.General.EggProbability = 12;
        config.Roosters[RoosterType.White].ProduceEntries = new List<ProduceEntry>
        {
            new() { Choice = "999", Probability = 100 }
        };

        var changed = manager.ApplyToConfig(config, catalogs, isNewConfig: false, CatalogApplyMode.Authoritative, monitor);

        Assert.True(changed);

        var expectedGeneralEggP = catalogs.Hatching.General!.EggProbability!.Value;
        Assert.Equal(expectedGeneralEggP, config.General.EggProbability);

        AssertEntriesMatch(config.Roosters[RoosterType.White].ProduceEntries, catalogs.Produce.Roosters["White"].Produce);
        AssertEntriesMatch(config.Roosters[RoosterType.White].DeluxeProduceEntries, catalogs.Produce.Roosters["White"].Deluxe);
    }

    private static void AssertEntriesMatch(IReadOnlyList<ProduceEntry> actual, IReadOnlyList<ProduceEntryDto> expected)
    {
        Assert.Equal(expected.Count, actual.Count);

        for (var i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Choice, actual[i].Choice);
            Assert.Equal(expected[i].CustomItemName, actual[i].CustomItemName);
            Assert.Equal(expected[i].Probability, actual[i].Probability);
        }
    }
}
