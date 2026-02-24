using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Machines;
using static LetMeHatch.EnumsAndStrings.Strings;

namespace LetMeHatch.Infrastructure.Content.Editors;

/// <summary>Clones machine rules for cloned/fertilized egg items.</summary>
public sealed class MachinesRulesEditor : LmhAssetEditorBase
{
    public override string AssetName => Mikew.NestCore.EnumsAndStrings.Strings.AssetNames.Machines;

    public override AssetEditPriority Priority => AssetEditPriority.Late + BASE_PRIORITY;

    public override string DebugGates => "App.MachinesEdits.TryCloneMachineRules(...).";

    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, MachineData>().Data;
        context.App.MachinesEdits.TryCloneMachineRules(
            data: (System.Collections.Generic.Dictionary<string, MachineData>)data,
            onlyFertilizedEggs: context.Config.OnlyFertilizedEggs,
            cloneMachineRules: context.Config.CloneMachineRules
        );
    }
}
