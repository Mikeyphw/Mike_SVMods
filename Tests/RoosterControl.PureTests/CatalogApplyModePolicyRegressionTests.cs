using RoosterControl.Application.Catalogs;
using RoosterControl.Enums;
using RoosterControl.Framework.Config;
using Xunit;

namespace RoosterControl.PureTests;

public sealed class CatalogApplyModePolicyRegressionTests
{
    [Fact]
    public void SeedOnly_ExistingConfig_DoesNotRecreateMissingRoosters()
    {
        var config = RoosterControlConfig.CreateDefault();
        config.Roosters.Remove(RoosterType.Blue);

        var changed = CatalogApplicator.ApplyToConfig(
            config, CatalogDefaults.CreateDefaults(), isNewConfig: false, CatalogApplyMode.SeedOnly);

        Assert.False(changed);
        Assert.False(config.Roosters.ContainsKey(RoosterType.Blue));
    }

    [Fact]
    public void SeedOnly_NewConfig_AppliesSuppliedCatalogValues()
    {
        var catalogs = CatalogDefaults.CreateDefaults();
        catalogs.Hatching.General!.EggProbability = 77;
        var config = RoosterControlConfig.CreateDefault();
        config.General.EggProbability = 12;

        var changed = CatalogApplicator.ApplyToConfig(
            config, catalogs, isNewConfig: true, CatalogApplyMode.SeedOnly);

        Assert.True(changed);
        Assert.Equal(77, config.General.EggProbability);
    }

    [Fact]
    public void Authoritative_OverridesProvidedGeneralAndRoosterFields()
    {
        var catalogs = CatalogDefaults.CreateDefaults();
        catalogs.Hatching.General!.EggProbability = 65;
        catalogs.Hatching.General.EnableDebug = false;
        catalogs.Hatching.General.BoostByRooster = 4.5f;
        catalogs.Hatching.Roosters["White"].Price = 9100;
        catalogs.Hatching.Roosters["White"].Buyable = false;
        catalogs.Hatching.Roosters["White"].MatureDays = 11;

        var config = RoosterControlConfig.CreateDefault();
        var changed = CatalogApplicator.ApplyToConfig(
            config, catalogs, isNewConfig: false, CatalogApplyMode.Authoritative);

        Assert.True(changed);
        Assert.Equal(65, config.General.EggProbability);
        Assert.False(config.General.EnableDebug);
        Assert.Equal(4.5f, config.General.BoostByRooster);
        Assert.Equal(9100, config.Roosters[RoosterType.White].Price);
        Assert.False(config.Roosters[RoosterType.White].Buyable);
        Assert.Equal(11, config.Roosters[RoosterType.White].MatureDays);
    }

    [Fact]
    public void Authoritative_OmittedOptionalValue_DoesNotOverwriteUserSetting()
    {
        var catalogs = CatalogDefaults.CreateDefaults();
        catalogs.Hatching.General!.EggProbability = null;
        catalogs.Hatching.Roosters["White"].Price = null;
        var config = RoosterControlConfig.CreateDefault();
        config.General.EggProbability = 53;
        config.Roosters[RoosterType.White].Price = 4300;

        CatalogApplicator.ApplyToConfig(
            config, catalogs, isNewConfig: false, CatalogApplyMode.Authoritative);

        Assert.Equal(53, config.General.EggProbability);
        Assert.Equal(4300, config.Roosters[RoosterType.White].Price);
    }

    [Fact]
    public void RepairInvalid_RepairsEmptyBlueEggChoice_PreservesValidCustomChoice()
    {
        var catalogs = CatalogDefaults.CreateDefaults();
        var config = RoosterControlConfig.CreateDefault();
        config.Roosters[RoosterType.Blue].BlueEggChoice = "";
        config.Roosters[RoosterType.White].BlueEggChoice = "MyCustomBlueEgg";

        var changed = CatalogApplicator.ApplyToConfig(
            config, catalogs, isNewConfig: false, CatalogApplyMode.RepairInvalid);

        Assert.True(changed);
        Assert.Equal(catalogs.Produce.Roosters["Blue"].BlueEggChoice,
            config.Roosters[RoosterType.Blue].BlueEggChoice);
        Assert.Equal("MyCustomBlueEgg", config.Roosters[RoosterType.White].BlueEggChoice);
    }
}
