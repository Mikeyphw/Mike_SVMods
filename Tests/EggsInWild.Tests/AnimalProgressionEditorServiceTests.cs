using System.Collections.Generic;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.Integrations.StardewUi.AnimalProgression;
using Xunit;

namespace EggsInWild.Tests;

public sealed class AnimalProgressionEditorServiceTests
{
    [Fact]
    public void CreateDraft_CopiesConfiguredModes()
    {
        var config = new ModConfig
        {
            AnimalProgressionMode = new Dictionary<string, bool>
            {
                ["Chicken"] = true,
                ["Duck"] = false,
            }
        };

        var draft = new AnimalProgressionEditorService().CreateDraft(config);

        Assert.True(draft.ConfiguredModes["Chicken"]);
        Assert.False(draft.ConfiguredModes["Duck"]);
    }

    [Fact]
    public void ApplyDraft_StoresOnlyNonDefaultOverrides()
    {
        var config = new ModConfig();
        var catalog = new[]
        {
            new AnimalProgressionAnimalRecord("Chicken", "Chicken", true, true, true),
            new AnimalProgressionAnimalRecord("Duck", "Duck", false, false, false),
        };
        var draft = new AnimalProgressionDraft(new Dictionary<string, bool>
        {
            ["Chicken"] = true,
            ["Duck"] = true,
        });

        new AnimalProgressionEditorService().ApplyDraft(config, draft, catalog);

        Assert.DoesNotContain("Chicken", config.AnimalProgressionMode.Keys);
        Assert.True(config.AnimalProgressionMode["Duck"]);
    }
}
