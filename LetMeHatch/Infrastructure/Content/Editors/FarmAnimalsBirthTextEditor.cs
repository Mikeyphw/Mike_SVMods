using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.FarmAnimals;
using static LetMeHatch.EnumsAndStrings.Strings;

namespace LetMeHatch.Infrastructure.Content.Editors;

/// <summary>Edits birth text shown when animals are born.</summary>
public sealed class FarmAnimalsBirthTextEditor : LmhAssetEditorBase
{
    public override string AssetName => Mikew.NestCore.EnumsAndStrings.Strings.AssetNames.FarmAnimals;

    public override AssetEditPriority Priority => AssetEditPriority.Late + EDIT_BIRTH_TEXT_PRIORITY;

    public override string DebugGates => "Config.EditBirthText == true.";

    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, FarmAnimalData>().Data;
        context.App.FarmAnimalsEdits.TryEditBirthText(
            data: (System.Collections.Generic.Dictionary<string, FarmAnimalData>)data,
            enabled: context.Config.EditBirthText
        );
    }
}
