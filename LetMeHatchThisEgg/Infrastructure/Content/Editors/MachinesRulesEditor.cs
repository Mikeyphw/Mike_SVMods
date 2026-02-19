using LetMeHatchThisEgg.GameAssetManagement;
using StardewModdingAPI.Events;
using StardewValley.GameData.Machines;
using static LetMeHatchThisEgg.EnumsAndStrings.Strings;

namespace LetMeHatchThisEgg.Infrastructure.Content.Editors;

/// <summary>Clones machine rules for cloned/fertilized egg items.</summary>
public sealed class MachinesRulesEditor : AssetEditorBase
{
    private readonly GameAssetManager _assets;

    public MachinesRulesEditor(GameAssetManager assets)
    {
        _assets = assets;
    }

    public override string AssetName => GameAssets.Machines;

    public override AssetEditPriority Priority => AssetEditPriority.Late + BASE_PRIORITY;

        public override string DebugGates => "(OnlyFertilizedEggs && HasAddedFertilizedEggs) || (!OnlyFertilizedEggs && HasAddedClonedItems && CloneMachineRules).";

public override void Edit(IAssetData asset, ContentContext context)
    {
        var cfg = context.Config;

        var shouldClone =
            (cfg.OnlyFertilizedEggs && _assets.HasAddedFertilizedEggs)
            || (!cfg.OnlyFertilizedEggs && _assets.HasAddedClonedItems && cfg.CloneMachineRules);

        if (!shouldClone)
            return;

        var data = asset.AsDictionary<string, MachineData>().Data;
        _assets.MachineEditor.CloneMachineData((System.Collections.Generic.Dictionary<string, MachineData>)data);
    }
}
