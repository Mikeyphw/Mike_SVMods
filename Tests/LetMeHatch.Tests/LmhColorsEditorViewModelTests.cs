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

        viewModel.ShowClonedEggs();
        viewModel.OverrideRows[0].SelectOverrideRow();

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
        viewModel.Save();

        Assert.NotNull(saved);
        Assert.False(saved!.PaletteSelections["egg.white"]["blue"]);
        Assert.Equal(new Color(0x22, 0x33, 0x44), saved.OverrideColors["egg.void"]);
    }

    [Fact]
    public void ClonedEggs_Save_PersistsManualOverrideOnly()
    {
        LmhColorsEditorDraft? saved = null;
        var viewModel = CreateViewModel(draft => saved = draft);

        viewModel.ShowClonedEggs();
        viewModel.OverrideRows[0].SelectOverrideRow();
        viewModel.SelectedOverrideColorText = "#AABBCC";
        viewModel.Save();

        Assert.NotNull(saved);
        Assert.Equal(new Color(0xAA, 0xBB, 0xCC), saved!.OverrideColors["egg.void"]);
    }

    private static LmhColorsEditorViewModel CreateViewModel(Action<LmhColorsEditorDraft>? onSave = null)
        => new(
            new LmhColorsEditorService(),
            () => new LmhColorsEditorCatalog(
                new[]
                {
                    new LmhColorPaletteGroupRecord("blue", "Blue", new Color(0x44, 0x88, 0xCC)),
                    new LmhColorPaletteGroupRecord("green", "Green", new Color(0x44, 0xAA, 0x66)),
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
                        Color.Transparent,
                        false),
                },
                new[]
                {
                    new LmhColorOverrideRecord(
                        "egg.void",
                        "Void Egg",
                        new Dictionary<string, bool>
                        {
                            ["blue"] = false,
                            ["green"] = true,
                        },
                        new Color(0x22, 0x33, 0x44),
                        true),
                }),
            onSave ?? (_ => { }));
}
