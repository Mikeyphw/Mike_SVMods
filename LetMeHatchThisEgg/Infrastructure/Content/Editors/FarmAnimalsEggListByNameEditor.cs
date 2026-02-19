using StardewModdingAPI.Events;
using StardewValley.GameData.FarmAnimals;
using static LetMeHatchThisEgg.EnumsAndStrings.Strings;

namespace LetMeHatchThisEgg.Infrastructure.Content.Editors;

/// <summary>Edits EggItemIds based on egg names for configured animals.</summary>
public sealed class FarmAnimalsEggListByNameEditor : LmhAssetEditorBase
{
    public override string AssetName => Mikew.BirdsOfAFeather.EnumsAndStrings.Strings.AssetNames.FarmAnimals;

    public override AssetEditPriority Priority => AssetEditPriority.Late + EDIT_EGG_LIST_PRIORITY;

    public override string DebugGates => "Config.EditEggListByName == true.";

    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, FarmAnimalData>().Data;
        context.App.FarmAnimalsEdits.TryEditEggListByName(
            data: (System.Collections.Generic.Dictionary<string, FarmAnimalData>)data,
            enabled: context.Config.EditEggListByName
        );
    }
}
