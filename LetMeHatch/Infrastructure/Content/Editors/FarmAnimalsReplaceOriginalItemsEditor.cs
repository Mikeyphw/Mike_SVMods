using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.FarmAnimals;
using static LetMeHatch.EnumsAndStrings.Strings;

namespace LetMeHatch.Infrastructure.Content.Editors;

/// <summary>Ensures clone items are associated with the correct original items for egg colors and relations.</summary>
public sealed class FarmAnimalsReplaceOriginalItemsEditor : LmhAssetEditorBase
{
    public override string AssetName => Mikew.NestCore.EnumsAndStrings.Strings.AssetNames.FarmAnimals;

    public override AssetEditPriority Priority => AssetEditPriority.Late + NEW_EGGS_ADD_PRIORITY;

    public override string DebugGates => "App.FarmAnimalsEdits.TryReplaceOriginalItemsForClones(...).";

    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, FarmAnimalData>().Data;
        context.App.FarmAnimalsEdits.TryReplaceOriginalItemsForClones((System.Collections.Generic.Dictionary<string, FarmAnimalData>)data);
    }
}
