using StardewModdingAPI.Events;
using StardewValley.GameData.FarmAnimals;

namespace LetMeHatchThisEgg.Infrastructure.Content.Editors;

/// <summary>Applies the clone animal edits (adds male clone entries).</summary>
public sealed class FarmAnimalsClonesEditor : LmhAssetEditorBase
{
    public override string AssetName => Mikew.BirdsOfAFeather.EnumsAndStrings.Strings.AssetNames.FarmAnimals;

    public override AssetEditPriority Priority => AssetEditPriority.Late;

    public override string DebugGates => "App.FarmAnimalsEdits.TryApplyCloneEdits(...).";

    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, FarmAnimalData>().Data;
        context.App.FarmAnimalsEdits.TryApplyCloneEdits(
            data: (System.Collections.Generic.Dictionary<string, FarmAnimalData>)data,
            stabilizeOrder: context.Config.StabilizeFarmAnimalsAssetOrder
        );
    }
}
