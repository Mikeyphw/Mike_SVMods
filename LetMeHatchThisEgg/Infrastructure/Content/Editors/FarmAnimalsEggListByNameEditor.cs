using LetMeHatchThisEgg.GameAssetManagement;
using StardewModdingAPI.Events;
using StardewValley.GameData.FarmAnimals;
using static LetMeHatchThisEgg.EnumsAndStrings.Strings;

namespace LetMeHatchThisEgg.Infrastructure.Content.Editors;

/// <summary>Edits EggItemIds based on egg names for configured animals.</summary>
public sealed class FarmAnimalsEggListByNameEditor : AssetEditorBase
{
    private readonly GameAssetManager _assets;

    public FarmAnimalsEggListByNameEditor(GameAssetManager assets)
    {
        _assets = assets;
    }

    public override string AssetName => GameAssets.FarmAnimals;

    public override AssetEditPriority Priority => AssetEditPriority.Late + EDIT_EGG_LIST_PRIORITY;

        public override string DebugGates => "Config.EditEggListByName == true.";

public override void Edit(IAssetData asset, ContentContext context)
    {
        if (!context.Config.EditEggListByName)
            return;

        var data = asset.AsDictionary<string, FarmAnimalData>().Data;
        _assets.EggListEditor.SetUpEggList(data);
    }
}
