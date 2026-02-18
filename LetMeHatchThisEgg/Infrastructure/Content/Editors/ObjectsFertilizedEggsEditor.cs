using LetMeHatchThisEgg.GameAssetManagement;
using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;
using static LetMeHatchThisEgg.EnumsAndStrings.Strings;

namespace LetMeHatchThisEgg.Infrastructure.Content.Editors;

/// <summary>Adds fertilized egg variants to Data/Objects.</summary>
public sealed class ObjectsFertilizedEggsEditor : AssetEditorBase
{
    private readonly GameAssetManager _assets;

    public ObjectsFertilizedEggsEditor(GameAssetManager assets)
    {
        _assets = assets;
    }

    public override string AssetName => GameAssets.Objects;

    public override AssetEditPriority Priority => AssetEditPriority.Late + NEW_EGGS_ADD_PRIORITY;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        if (_assets.ItemsToAdd.Count < 1)
            return;

        var data = asset.AsDictionary<string, ObjectData>().Data;
        _assets.ItemManager.EditNewFertilizedEggs(data);
        _assets.TextureManager.ProcessClonedTexturesWithGrayColor(data);
    }
}
