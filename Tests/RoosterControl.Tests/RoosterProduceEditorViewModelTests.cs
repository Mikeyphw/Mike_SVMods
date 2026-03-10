using RoosterControl.Enums;
using RoosterControl.Framework.Config;
using RoosterControl.Integrations.StardewUi.Produce;
using Xunit;

namespace RoosterControl.Tests;

public sealed class RoosterProduceEditorViewModelTests
{
    [Fact]
    public void Constructor_UsesRequestedRoosterAndSection()
    {
        var viewModel = CreateViewModel(initialRoosterType: RoosterType.Void, initialSection: RoosterProduceSection.Deluxe);

        Assert.Equal("Void", viewModel.SelectedRoosterName);
        Assert.True(viewModel.IsDeluxeSection);
    }

    [Fact]
    public void Save_PersistsModeAndSlotEdits()
    {
        var saves = 0;
        var config = new RoosterControlConfig();
        var viewModel = CreateViewModel(config, () => saves++);

        viewModel.SelectedProduceMode = ProduceMode.KeepOriginalProduce.ToString();
        viewModel.CurrentSlots[0].SelectedChoice = "Brown Egg";
        viewModel.CurrentSlots[0].Weight = 41;
        viewModel.Save();

        Assert.Equal(1, saves);
        Assert.Equal(ProduceMode.KeepOriginalProduce, config.Roosters[RoosterType.White].ProduceOption);
        Assert.Equal("Brown Egg", config.Roosters[RoosterType.White].ProduceEntries[0].Choice);
        Assert.Equal(41, config.Roosters[RoosterType.White].ProduceEntries[0].Probability);
        Assert.False(viewModel.CanSave);
    }

    [Fact]
    public void Discard_RestoresSnapshotValues()
    {
        var viewModel = CreateViewModel();

        viewModel.CurrentSlots[0].SelectedChoice = "Brown Egg";
        Assert.True(viewModel.CanSave);

        viewModel.Discard();

        Assert.False(viewModel.CanSave);
        Assert.Equal("Custom", viewModel.CurrentSlots[0].SelectedChoice);
    }

    private static RoosterProduceEditorViewModel CreateViewModel(
        RoosterControlConfig? config = null,
        System.Action? onSave = null,
        RoosterType initialRoosterType = RoosterType.White,
        RoosterProduceSection initialSection = RoosterProduceSection.Normal)
    {
        config ??= new RoosterControlConfig();
        onSave ??= () => { };

        return new RoosterProduceEditorViewModel(
            new RoosterProduceEditorService(),
            config,
            onSave,
            initialRoosterType,
            initialSection);
    }
}
