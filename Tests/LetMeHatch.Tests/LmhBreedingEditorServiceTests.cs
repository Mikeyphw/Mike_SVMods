using LetMeHatch.Config;
using LetMeHatch.Integrations.StardewUi.Breeding;
using LetMeHatch.Models;
using Xunit;
using static LetMeHatch.EnumsAndStrings.Enums;

namespace LetMeHatch.Tests;

public sealed class LmhBreedingEditorServiceTests
{
    private readonly LmhBreedingEditorService _service = new();

    [Fact]
    public void LoadPacksSnapshot_OrdersByDisplayNameThenFemaleKey()
    {
        var packs = _service.LoadPacksSnapshot(
            new Dictionary<string, Dictionary<string, BreedingDataFemale>>(StringComparer.OrdinalIgnoreCase)
            {
                ["pack.beta"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["VoidChicken"] = new(),
                    ["BlueChicken"] = new(),
                },
                ["pack.alpha"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["WhiteChicken"] = new(),
                },
            },
            Array.Empty<StardewModdingAPI.IContentPack>());

        Assert.Collection(
            packs,
            pack =>
            {
                Assert.Equal("pack.alpha", pack.ContentPackId);
                Assert.Equal(new[] { "WhiteChicken" }, pack.FemaleKeys);
            },
            pack =>
            {
                Assert.Equal("pack.beta", pack.ContentPackId);
                Assert.Equal(new[] { "BlueChicken", "VoidChicken" }, pack.FemaleKeys);
            });
    }

    [Fact]
    public void TryLoadFemaleRecord_OrdersMaleKeysAndUsesConfigValues()
    {
        var femaleData = new BreedingDataFemale
        {
            Males = new Dictionary<string, BreedingDataMale>(StringComparer.OrdinalIgnoreCase)
            {
                ["VoidChicken_Male"] = new() { AdjustableMaleChances = false, MaleChance = 0.2f, BoostByMale = 0.3f },
                ["BlueChicken"] = new() { AdjustableMaleChances = true, MaleChance = 0.4f, BoostByMale = 0.6f },
            }
        };
        var femaleConfig = new BreedingConfig(femaleData);
        femaleConfig.Enabled["BlueChicken"] = false;
        femaleConfig.MaleChance["BlueChicken"] = 0.9f;

        var loaded = _service.TryLoadFemaleRecord(
            new Dictionary<string, Dictionary<string, BreedingDataFemale>>(StringComparer.OrdinalIgnoreCase)
            {
                ["pack.alpha"] = new(StringComparer.OrdinalIgnoreCase) { ["WhiteChicken"] = femaleData }
            },
            new Dictionary<string, Dictionary<string, BreedingConfig>>(StringComparer.OrdinalIgnoreCase)
            {
                ["pack.alpha"] = new(StringComparer.OrdinalIgnoreCase) { ["WhiteChicken"] = femaleConfig }
            },
            "pack.alpha",
            "WhiteChicken",
            out var record);

        Assert.True(loaded);
        Assert.Equal(new[] { "BlueChicken", "VoidChicken_Male" }, record.Males.Select(x => x.MaleKey).ToArray());
        Assert.False(record.Males[0].Enabled);
        Assert.Equal(0.9f, record.Males[0].MaleChance);
    }

    [Fact]
    public void ApplyDraft_UpdatesPerFemaleBreedingValues()
    {
        var femaleData = new BreedingDataFemale();
        femaleData.Males["BlueChicken"] = new BreedingDataMale();
        var femaleConfig = new BreedingConfig(femaleData);
        var draft = new LmhBreedingDraft(
            ApplyDailyLuckBoost: false,
            EggChances: new Dictionary<ItemType, float>
            {
                [ItemType.Normal] = 0.1f,
                [ItemType.Large] = 0.2f,
                [ItemType.FertilizedEgg] = 0.3f,
            },
            Enabled: new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                ["BlueChicken"] = false,
            },
            MaleChance: new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
            {
                ["BlueChicken"] = 0.7f,
            },
            BoostByMale: new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
            {
                ["BlueChicken"] = 0.8f,
            },
            AddAsAlternatePurchase: false);

        _service.ApplyDraft(femaleData, femaleConfig, draft);

        Assert.False(femaleData.ApplyDailyLuckBoost);
        Assert.False(femaleConfig.ApplyDailyLuckBoost);
        Assert.Equal(0.1f, femaleConfig.EggChances[ItemType.Normal]);
        Assert.False(femaleConfig.Enabled["BlueChicken"]);
        Assert.Equal(0.7f, femaleConfig.MaleChance["BlueChicken"]);
        Assert.Equal(0.8f, femaleConfig.BoostByMale["BlueChicken"]);
        Assert.False(femaleConfig.MaleConfig.AddAsAlternatePurchase);
    }

    [Fact]
    public void ApplyDraft_NoOpDraftPreservesExistingValues()
    {
        var femaleData = new BreedingDataFemale();
        femaleData.Males["BlueChicken"] = new BreedingDataMale();
        var femaleConfig = new BreedingConfig(femaleData);
        var draft = _service.CreateDraft(femaleData, femaleConfig);

        _service.ApplyDraft(femaleData, femaleConfig, draft);

        Assert.True(femaleData.ApplyDailyLuckBoost);
        Assert.Equal(0.4f, femaleConfig.EggChances[ItemType.Normal]);
        Assert.True(femaleConfig.Enabled["BlueChicken"]);
        Assert.Equal(0.5f, femaleConfig.MaleChance["BlueChicken"]);
        Assert.Equal(0.5f, femaleConfig.BoostByMale["BlueChicken"]);
        Assert.True(femaleConfig.MaleConfig.AddAsAlternatePurchase);
    }

}
