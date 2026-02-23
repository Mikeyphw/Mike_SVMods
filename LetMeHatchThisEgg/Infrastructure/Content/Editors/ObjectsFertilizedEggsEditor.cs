using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;
using static LetMeHatchThisEgg.EnumsAndStrings.Strings;

namespace LetMeHatchThisEgg.Infrastructure.Content.Editors;

/// <summary>Adds fertilized egg variants to Data/Objects.</summary>
public sealed class ObjectsFertilizedEggsEditor : LmhAssetEditorBase
{
    public override string AssetName => Mikew.BirdsOfAFeather.EnumsAndStrings.Strings.AssetNames.Objects;

    public override AssetEditPriority Priority => AssetEditPriority.Late + NEW_EGGS_ADD_PRIORITY;

    public override string DebugGates => "App.ObjectsEdits.TryAddFertilizedEggObjects(...).";

    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, ObjectData>().Data;
        context.App.ObjectsEdits.TryAddFertilizedEggObjects((System.Collections.Generic.Dictionary<string, ObjectData>)data);
    }
}
