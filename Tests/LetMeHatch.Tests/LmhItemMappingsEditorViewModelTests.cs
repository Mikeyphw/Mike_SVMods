using LetMeHatch.Integrations.StardewUi.Advanced;
using Xunit;
using static LetMeHatch.EnumsAndStrings.Enums;

namespace LetMeHatch.Tests;

public sealed class LmhItemMappingsEditorViewModelTests
{
    [Fact]
    public void OpenGroup_SelectsFirstMappingInGroup()
    {
        var viewModel = CreateViewModel();

        viewModel.MappingGroups[1].OpenGroup();

        Assert.Equal("Milk", viewModel.SelectedGroup);
        Assert.Equal("Cow", viewModel.SelectedMapping);
        Assert.True(viewModel.HasSelectedMapping);
        Assert.Single(viewModel.CurrentMappings);
    }

    [Fact]
    public void OpenMapping_UpdatesBaseAndEffectiveValues()
    {
        var viewModel = CreateViewModel();

        viewModel.CurrentMappings[0].OpenMapping();

        Assert.Equal("(O)176", viewModel.BaseNormalItemText);
        Assert.Equal("(O)174", viewModel.BaseLargeItemText);
        Assert.Equal("(O)999", viewModel.EffectiveNormalItemText);
        Assert.Equal("(O)174", viewModel.EffectiveLargeItemText);
    }

    [Fact]
    public void Save_PersistsEditedOverrides()
    {
        LmhItemMappingsEditorDraft? saved = null;
        var viewModel = CreateViewModel(draft => saved = draft);

        viewModel.CurrentMappings[0].OpenMapping();
        viewModel.NormalOverrideText = "(O)555";
        viewModel.LargeOverrideText = "(O)556";
        viewModel.Save();

        Assert.NotNull(saved);
        Assert.Equal("(O)555", saved!.Mappings[CloneItemType.Egg]["Chicken"].NormalItemId);
        Assert.Equal("(O)556", saved.Mappings[CloneItemType.Egg]["Chicken"].LargeItemId);
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
                            new LmhItemMappingRecord("Chicken", "Chicken", "(O)176", "(O)174", "(O)999", string.Empty, "(O)999", "(O)174"),
                        }),
                    new LmhItemMappingsGroupRecord(
                        CloneItemType.Milk,
                        "Milk",
                        new[]
                        {
                            new LmhItemMappingRecord("Cow", "Cow", "(O)184", "(O)186", string.Empty, "(O)888", "(O)184", "(O)888"),
                        }),
                }),
            onSave ?? (_ => { }));
}
