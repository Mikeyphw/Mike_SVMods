using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.FarmAnimals;

namespace LetMeHatchThisEgg.Infrastructure.Content.Editors;

/// <summary>Adds placeholder entries for male clone animals before later edits fill them in.</summary>
public sealed class FarmAnimalsClonePlaceholdersEditor : LmhAssetEditorBase
{
    public override string AssetName => Mikew.BirdsOfAFeather.EnumsAndStrings.Strings.AssetNames.FarmAnimals;

    public override AssetEditPriority Priority => AssetEditPriority.Early;

    public override string DebugGates => "App.FarmAnimalsEdits.TryAddClonePlaceholders(...).";

    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, FarmAnimalData>().Data;
        var changed = context.App.FarmAnimalsEdits.TryAddClonePlaceholders((System.Collections.Generic.Dictionary<string, FarmAnimalData>)data);

        if (changed)
            context.Log.Log("Added clone placeholders (FarmAnimals).", LogLevel.Trace);
    }
}
