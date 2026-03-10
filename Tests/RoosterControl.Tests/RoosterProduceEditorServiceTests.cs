using System.Linq;
using RoosterControl.Enums;
using RoosterControl.Framework.Config;
using RoosterControl.Integrations.StardewUi.Produce;
using Xunit;

namespace RoosterControl.Tests;

public sealed class RoosterProduceEditorServiceTests
{
    [Fact]
    public void LoadSnapshot_IncludesAllRoosterTypesAndModes()
    {
        var config = new RoosterControlConfig();
        config.Roosters[RoosterType.Blue].ProduceOption = ProduceMode.KeepOriginalProduce;
        config.Roosters[RoosterType.Gold].DeluxeProduceOption = DeluxeProduceMode.Off;

        var snapshot = new RoosterProduceEditorService().LoadSnapshot(config);

        Assert.Equal(5, snapshot.Roosters.Count);
        Assert.Equal(ProduceMode.KeepOriginalProduce, snapshot.Roosters.Single(r => r.Type == RoosterType.Blue).ProduceMode);
        Assert.Equal(DeluxeProduceMode.Off, snapshot.Roosters.Single(r => r.Type == RoosterType.Gold).DeluxeProduceMode);
    }

    [Fact]
    public void ChoiceOptions_MatchGmcmAllowedValueSources()
    {
        var snapshot = new RoosterProduceEditorService().LoadSnapshot(new RoosterControlConfig());

        Assert.Contains(snapshot.ChoiceOptions, option => option.Key == "Custom");
        Assert.Contains(snapshot.ChoiceOptions, option => option.Key == "White Egg");
        Assert.DoesNotContain(snapshot.ChoiceOptions, option => option.Key == "Random Color Egg");
    }

    [Fact]
    public void ApplyDraft_PersistsNormalAndDeluxeSlots()
    {
        var config = new RoosterControlConfig();
        var service = new RoosterProduceEditorService();
        var draft = service.CreateDraft(service.LoadSnapshot(config));

        var white = draft.Roosters[RoosterType.White];
        white.ProduceMode = ProduceMode.CustomizeBelow;
        white.DeluxeProduceMode = DeluxeProduceMode.CustomizeBelow;
        white.NormalSlots[0].Choice = "Brown Egg";
        white.NormalSlots[0].Weight = 33;
        white.DeluxeSlots[1].Choice = "Large White Egg";
        white.DeluxeSlots[1].Weight = 77;

        service.ApplyDraft(config, draft);

        Assert.Equal("Brown Egg", config.Roosters[RoosterType.White].ProduceEntries[0].Choice);
        Assert.Equal(33, config.Roosters[RoosterType.White].ProduceEntries[0].Probability);
        Assert.Equal("Large White Egg", config.Roosters[RoosterType.White].DeluxeProduceEntries[1].Choice);
        Assert.Equal(77, config.Roosters[RoosterType.White].DeluxeProduceEntries[1].Probability);
    }
}
