using System.Collections.Generic;
using EggsInWild.Infrastructure.Integrations.StardewUi.AnimalProgression;
using Xunit;

namespace EggsInWild.Tests;

public sealed class AnimalProgressionEditorViewModelTests
{
    [Fact]
    public void Search_FiltersAnimalsByDisplayName()
    {
        var viewModel = CreateViewModel();

        viewModel.SearchText = "Duck";

        Assert.Single(viewModel.Animals);
        Assert.Equal("Duck", viewModel.Animals[0].ButtonText);
    }

    [Fact]
    public void Save_PersistsSelectedMode()
    {
        AnimalProgressionDraft? savedDraft = null;
        var viewModel = CreateViewModel((draft, _) => savedDraft = draft);

        viewModel.ModeOptions[2].SelectMode();
        viewModel.Save();

        Assert.NotNull(savedDraft);
        Assert.False(savedDraft!.ConfiguredModes["Chicken"]);
    }

    private static AnimalProgressionEditorViewModel CreateViewModel(Action<AnimalProgressionDraft, IReadOnlyList<AnimalProgressionAnimalRecord>>? onSave = null)
        => new(
            new AnimalProgressionEditorService(),
            () => new[]
            {
                new AnimalProgressionAnimalRecord("Chicken", "Chicken", true, true, true),
                new AnimalProgressionAnimalRecord("Duck", "Duck", false, false, false),
            },
            () => new AnimalProgressionDraft(new Dictionary<string, bool>()),
            onSave ?? ((_, _) => { }));
}
