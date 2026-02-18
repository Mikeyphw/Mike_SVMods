using LetMeHatchThisEgg.GameAssetManagement;
using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;

namespace LetMeHatchThisEgg.Infrastructure.Content.Editors;

/// <summary>Adds cloned egg objects to Data/Objects.</summary>
public sealed class ObjectsClonedEggsEditor : AssetEditorBase
{
    private readonly GameAssetManager _assets;

    public ObjectsClonedEggsEditor(GameAssetManager assets)
    {
        _assets = assets;
    }

    public override string AssetName => GameAssets.Objects;

    public override AssetEditPriority Priority => AssetEditPriority.Default;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        if (_assets.ItemsToAdd.Count < 1)
            return;

        var data = asset.AsDictionary<string, ObjectData>().Data;
        _assets.ItemManager.EditNewClonedEggs(data);

        if (!context.Config.OnlyFertilizedEggs)
            _assets.TextureManager.ProcessClonedTexturesWithGrayColor(data);
    }
}
