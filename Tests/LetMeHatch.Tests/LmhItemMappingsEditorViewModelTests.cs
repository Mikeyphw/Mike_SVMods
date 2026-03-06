using System.Collections.Generic;
using LetMeHatch.Integrations.StardewUi.Advanced;
using Xunit;
using static LetMeHatch.EnumsAndStrings.Enums;

namespace LetMeHatch.Tests;

public sealed class LmhItemMappingsEditorViewModelTests
{
    [Fact]
    public void OpenGroup_SelectsFirstMapping()
    {
        var viewModel = CreateViewModel();

        viewModel.MappingGroups[1].OpenGroup();

        Assert.Equal("Milk", viewModel.SelectedGroup);
        Assert.Equal("Cow", viewModel.SelectedMapping);
        Assert.True(viewModel.HasSelectedMapping);
    }

    [Fact]
    public void Save_PersistsEditedMapping()
    {
        LmhItemMappingsEditorDraft? saved = null;
        var viewModel = CreateViewModel(draft => saved = draft);

        viewModel.MappingGroups[0].OpenGroup();
        viewModel.NormalItemText = "(O)999";
        viewModel.LargeItemText = "(O)998";
        viewModel.Save();

        Assert.NotNull(saved);
        Assert.Equal("(O)999", saved!.Mappings[CloneItemType.Egg]["Chicken"].NormalItemId);
        Assert.Equal("(O)998", saved.Mappings[CloneItemType.Egg]["Chicken"].LargeItemId);
    }

    private static LmhItemMappingsEditorViewModel CreateViewModel(Action<LmhItemMappingsEditorDraft>? onSave = null)
        => new(
            new LmhItemMappingsEditorService(),
            () => new LmhItemMappingsEditorCatalog(
                new[]
                {
                    new LmhItemMappingsGroupRecord(
                        CloneItemType.Egg,
                        "Egg",
                        new[]
                        {
                            new LmhItemMappingRecord("Chicken", "(O)176", "(O)174"),
                        }),
                    new LmhItemMappingsGroupRecord(
                        CloneItemType.Milk,
                        "Milk",
                        new[]
                        {
                            new LmhItemMappingRecord("Cow", "(O)184", "(O)186"),
                        }),
                }),
            onSave ?? (_ => { }));
}
