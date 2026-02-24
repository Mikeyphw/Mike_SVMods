using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;

namespace LetMeHatch.Infrastructure.Content.Editors;

/// <summary>Adds cloned egg objects to Data/Objects.</summary>
public sealed class ObjectsClonedEggsEditor : LmhAssetEditorBase
{
    public override string AssetName => Mikew.NestCore.EnumsAndStrings.Strings.AssetNames.Objects;

    public override AssetEditPriority Priority => AssetEditPriority.Default;

    public override string DebugGates => "App.ObjectsEdits.TryAddClonedEggObjects(...).";

    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, ObjectData>().Data;
        context.App.ObjectsEdits.TryAddClonedEggObjects(
            data: (System.Collections.Generic.Dictionary<string, ObjectData>)data,
            onlyFertilizedEggs: context.Config.OnlyFertilizedEggs
        );
    }
}
