using LetMeHatchThisEgg.GameAssetManagement;
using StardewModdingAPI.Events;
using StardewValley.GameData.FarmAnimals;
using static LetMeHatchThisEgg.EnumsAndStrings.Strings;

namespace LetMeHatchThisEgg.Infrastructure.Content.Editors;

/// <summary>Edits birth text shown when animals are born.</summary>
public sealed class FarmAnimalsBirthTextEditor : AssetEditorBase
{
    private readonly GameAssetManager _assets;

    public FarmAnimalsBirthTextEditor(GameAssetManager assets)
    {
        _assets = assets;
    }

    public override string AssetName => GameAssets.FarmAnimals;

    public override AssetEditPriority Priority => AssetEditPriority.Late + EDIT_BIRTH_TEXT_PRIORITY;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        if (!context.Config.EditBirthText)
            return;

        var data = asset.AsDictionary<string, FarmAnimalData>().Data;
        _assets.BirthTextEditor.EditBirthText(data);
    }
}
