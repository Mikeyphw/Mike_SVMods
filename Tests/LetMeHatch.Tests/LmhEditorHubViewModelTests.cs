using System;
using System.Collections.Generic;
using LetMeHatch.Config;
using LetMeHatch.Integrations.StardewUi;
using LetMeHatch.Integrations.StardewUi.Advanced;
using LetMeHatch.Integrations.StardewUi.Breeding;
using LetMeHatch.Integrations.StardewUi.Colors;
using Microsoft.Xna.Framework;
using Xunit;
using static LetMeHatch.EnumsAndStrings.Enums;

namespace LetMeHatch.Tests;

public sealed class LmhEditorHubViewModelTests
{
    [Fact]
    public void InitialTab_IsRespected()
    {
        var viewModel = CreateViewModel(LmhEditorHubTab.Mappings);

        Assert.True(viewModel.IsMappingsTab);
        Assert.False(viewModel.IsBreedingTab);
        Assert.False(viewModel.IsColorsTab);
    }

    [Fact]
    public void ShowColorsTab_SwitchesSelection()
    {
        var viewModel = CreateViewModel(LmhEditorHubTab.Breeding);

        viewModel.ShowColorsTab();

        Assert.True(viewModel.IsColorsTab);
        Assert.Equal("[Colors]", viewModel.ColorsTabText);
    }

    private static LmhEditorHubViewModel CreateViewModel(LmhEditorHubTab initialTab)
    {
        var config = new ModConfig();

        return new LmhEditorHubViewModel(
            new LmhBreedingEditorViewModel(
                new LmhBreedingEditorService(),
                () => new[]
                {
                    new LmhBreedingPackRecord("pack.alpha", "Alpha Pack", new[] { "WhiteChicken" }),
                },
                (pack, female) => new LmhBreedingFemaleRecord(
                    pack,
                    female,
                    true,
                    true,
                    new Dictionary<ItemType, float>
                    {
                        [ItemType.Normal] = 0.4f,
                        [ItemType.Large] = 0.6f,
                        [ItemType.FertilizedEgg] = 0.6f,
                    },
                    Array.Empty<LmhBreedingMaleRecord>()),
                (_, _, _) => { }),
            new LmhColorsEditorViewModel(
                new LmhColorsEditorService(),
                () => new LmhColorsEditorCatalog(
                    new[] { new LmhColorPaletteGroupRecord("blue", "Blue", new Color(0x44, 0x88, 0xCC)) },
                    new[]
                    {
                        new LmhColorOverrideRecord("egg.white", "White Egg", new Dictionary<string, bool> { ["blue"] = true }, Color.Transparent, false),
                    },
                    Array.Empty<LmhColorOverrideRecord>()),
                _ => { }),
            new LmhItemMappingsEditorViewModel(
                new LmhItemMappingsEditorService(),
                () => new LmhItemMappingsEditorCatalog(
                    new[]
                    {
                        new LmhItemMappingsGroupRecord(
                            CloneItemType.Egg,
                            "Egg",
                            new[] { new LmhItemMappingRecord("Chicken", "Chicken", "(O)176", "(O)174", string.Empty, string.Empty, "(O)176", "(O)174") }),
                    }),
                _ => { }),
            new LmhSettingsEditorViewModel(
                () => config,
                _ => { },
                () => Array.Empty<LmhSettingsAnimalOption>(),
                () => string.Empty,
                () => string.Empty,
                () => string.Empty),
            new LmhDebugEditorViewModel(
                () => config,
                _ => { }),
            initialTab);
    }
}
