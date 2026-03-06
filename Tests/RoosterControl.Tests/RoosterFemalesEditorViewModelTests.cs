using System;
using System.Collections.Generic;
using System.Linq;
using RoosterControl.Framework.Config;
using RoosterControl.Integrations.StardewUi.Females;
using Xunit;

namespace RoosterControl.Tests;

public sealed class RoosterFemalesEditorViewModelTests
{
    [Fact]
    public void SearchText_FiltersVisibleFemales()
    {
        var viewModel = CreateViewModel();

        viewModel.SearchText = "void";

        Assert.Single(viewModel.Females);
        Assert.Equal("Void Chicken", viewModel.SelectedFemale);
    }

    [Fact]
    public void SelectAssignment_UpdatesSaveFlagsAndDiscardRestoresSnapshot()
    {
        var viewModel = CreateViewModel();
        viewModel.OpenFemale(viewModel.Females.First(f => f.FemaleKey == "BlueChicken"));

        var option = viewModel.AssignmentOptions.Single(x => x.Value == FemaleRoosterAssignment.Gold);
        option.SelectAssignment();

        Assert.True(viewModel.CanSave);
        Assert.True(viewModel.CanDiscard);

        viewModel.Discard();

        Assert.False(viewModel.CanSave);
        Assert.False(viewModel.CanDiscard);
        Assert.Contains("Blue rooster", viewModel.Females.First(f => f.FemaleKey == "BlueChicken").MetaText);
    }

    [Fact]
    public void Save_AppliesAssignmentsAndInvokesCallback()
    {
        var saves = 0;
        var config = new RoosterControlConfig();
        var viewModel = CreateViewModel(config, () => saves++);
        viewModel.OpenFemale(viewModel.Females.First(f => f.FemaleKey == "BlueChicken"));

        viewModel.AssignmentOptions.Single(x => x.Value == FemaleRoosterAssignment.Gold).SelectAssignment();
        viewModel.Save();

        Assert.Equal(1, saves);
        Assert.Equal(FemaleRoosterAssignment.Gold, config.FemalesOptions["BlueChicken"]);
        Assert.False(viewModel.CanSave);
    }

    private static RoosterFemalesEditorViewModel CreateViewModel(RoosterControlConfig? config = null, Action? onSave = null)
    {
        config ??= new RoosterControlConfig();
        onSave ??= () => { };

        return new RoosterFemalesEditorViewModel(
            new RoosterFemalesEditorService(),
            config,
            CreateSnapshot,
            onSave);
    }

    private static RoosterFemalesEditorSnapshot CreateSnapshot()
        => new(
            new[]
            {
                new RoosterFemalesFemaleRecord(
                    "BlueChicken",
                    "Blue Chicken",
                    "Blue rooster | Normal: BlueEgg | Large: LargeBlueEgg",
                    "Normal: BlueEgg | Large: LargeBlueEgg",
                    "Key: BlueChicken",
                    FemaleRoosterAssignment.Blue,
                    CreateOptions(FemaleRoosterAssignment.Blue)),
                new RoosterFemalesFemaleRecord(
                    "VoidChicken",
                    "Void Chicken",
                    "Void rooster | Normal: VoidEgg | Large: LargeVoidEgg",
                    "Normal: VoidEgg | Large: LargeVoidEgg",
                    "Key: VoidChicken",
                    FemaleRoosterAssignment.Void,
                    CreateOptions(FemaleRoosterAssignment.Void)),
            },
            "Females: 2 | Assigned: 2");

    private static IReadOnlyList<RoosterFemalesAssignmentOptionRecord> CreateOptions(FemaleRoosterAssignment selected)
        => Enum.GetValues(typeof(FemaleRoosterAssignment))
            .Cast<FemaleRoosterAssignment>()
            .Select(value => new RoosterFemalesAssignmentOptionRecord(
                value,
                value.ToString(),
                value == selected ? "Current assignment" : $"Assign to {value} rooster"))
            .ToList();
}
