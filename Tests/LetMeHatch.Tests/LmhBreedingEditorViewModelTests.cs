using LetMeHatch.Integrations.StardewUi.Breeding;
using Xunit;
using static LetMeHatch.EnumsAndStrings.Enums;

namespace LetMeHatch.Tests;

public sealed class LmhBreedingEditorViewModelTests
{
    [Fact]
    public void OpenPackAndFemale_SelectsExpectedRecords()
    {
        var viewModel = CreateViewModel();

        viewModel.OpenPack(viewModel.Packs.Single(x => x.ContentPackId == "pack.beta"));
        viewModel.OpenFemale(viewModel.Females.Single(x => x.FemaleKey == "VoidChicken"));

        Assert.Equal("Beta Pack", viewModel.SelectedPack);
        Assert.Equal("VoidChicken", viewModel.SelectedFemale);
        Assert.True(viewModel.HasSelectedFemale);
    }

    [Fact]
    public void Save_PersistsEditedDraft()
    {
        string? savedPack = null;
        string? savedFemale = null;
        LmhBreedingDraft? savedDraft = null;

        var viewModel = CreateViewModel((pack, female, draft) =>
        {
            savedPack = pack;
            savedFemale = female;
            savedDraft = draft;
        });

        viewModel.ShowRulesTab();
        viewModel.NormalEggChanceText = "0.11";
        viewModel.LargeEggChanceText = "0.22";
        viewModel.FertilizedEggChanceText = "0.33";
        viewModel.OpenMale(viewModel.Males.First());
        viewModel.SelectedMaleChanceText = "0.77";
        viewModel.SelectedMaleBoostText = "0.88";
        viewModel.Save();

        Assert.Equal("pack.alpha", savedPack);
        Assert.Equal("WhiteChicken", savedFemale);
        Assert.NotNull(savedDraft);
        Assert.Equal(0.11f, savedDraft!.EggChances[ItemType.Normal]);
        Assert.Equal(0.77f, savedDraft.MaleChance["BlueChicken"]);
    }

    private static LmhBreedingEditorViewModel CreateViewModel(Action<string, string, LmhBreedingDraft>? onSave = null)
        => new(
            new LmhBreedingEditorService(),
            () => new[]
            {
                new LmhBreedingPackRecord("pack.alpha", "Alpha Pack", new[] { "WhiteChicken" }),
                new LmhBreedingPackRecord("pack.beta", "Beta Pack", new[] { "VoidChicken" }),
            },
            (pack, female) =>
            {
                if (pack == "pack.alpha" && female == "WhiteChicken")
                {
                    return new LmhBreedingFemaleRecord(
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
                        new[]
                        {
                            new LmhBreedingMaleRecord(
                                "BlueChicken",
                                "BlueChicken",
                                false,
                                true,
                                new Dictionary<ItemType, string?>(),
                                true,
                                0.5f,
                                0.5f,
                                false),
                        });
                }

                return new LmhBreedingFemaleRecord(
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
                    Array.Empty<LmhBreedingMaleRecord>());
            },
            onSave ?? ((_, _, _) => { }));
}
