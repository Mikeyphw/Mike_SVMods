using System.Collections.Generic;
using LetMeHatch.Integrations.StardewUi.Colors;
using Microsoft.Xna.Framework;
using Xunit;

namespace LetMeHatch.Tests;

public sealed class LmhColorsEditorViewModelTests
{
    [Fact]
    public void SelectOverrideRow_UpdatesPreview()
    {
        var viewModel = CreateViewModel();

        viewModel.OverrideRows[1].SelectOverrideRow();

        Assert.Equal("Void Egg", viewModel.PreviewLabel);
        Assert.Contains("#223344", viewModel.PreviewStatusText);
    }

    [Fact]
    public void Save_PersistsPaletteAndColorChanges()
    {
        LmhColorsEditorDraft? saved = null;
        var viewModel = CreateViewModel(draft => saved = draft);

        viewModel.OverrideRows[0].SelectOverrideRow();
        viewModel.PaletteGroups[0].OpenPaletteGroup();
        viewModel.ToggleSelectedPaletteGroup();
        viewModel.SelectedOverrideColorText = "#AABBCC";
        viewModel.Save();

        Assert.NotNull(saved);
        Assert.False(saved!.PaletteSelections["egg.white"]["blue"]);
        Assert.Equal(new Color(0xAA, 0xBB, 0xCC), saved.OverrideColors["egg.white"]);
    }

    private static LmhColorsEditorViewModel CreateViewModel(Action<LmhColorsEditorDraft>? onSave = null)
        => new(
            new LmhColorsEditorService(),
            () => new LmhColorsEditorCatalog(
                new[]
                {
                    new LmhColorPaletteGroupRecord("blue", "Blue"),
                    new LmhColorPaletteGroupRecord("green", "Green"),
                },
                new[]
                {
                    new LmhColorOverrideRecord(
                        "egg.white",
                        "White Egg",
                        new Dictionary<string, bool>
                        {
                            ["blue"] = true,
                            ["green"] = true,
                        },
                        Color.Transparent),
                    new LmhColorOverrideRecord(
                        "egg.void",
                        "Void Egg",
                        new Dictionary<string, bool>
                        {
                            ["blue"] = false,
                            ["green"] = true,
                        },
                        new Color(0x22, 0x33, 0x44)),
                }),
            onSave ?? (_ => { }));
}
