using LetMeHatchThisEgg.GameAssetManagement;
using StardewModdingAPI.Events;

namespace LetMeHatchThisEgg.Infrastructure.Content.Editors;

/// <summary>Applies the clone animal edits (adds male clone entries).</summary>
public sealed class FarmAnimalsClonesEditor : AssetEditorBase
{
    private readonly GameAssetManager _assets;

    public FarmAnimalsClonesEditor(GameAssetManager assets)
    {
        _assets = assets;
    }

    public override string AssetName => GameAssets.FarmAnimals;

    public override AssetEditPriority Priority => AssetEditPriority.Late;

    public override string DebugGates => "ClonedAnimalsInfoList.Count > 0.";

    public override void Edit(IAssetData asset, ContentContext context)
    {
        if (_assets.ClonedAnimalsInfoList.Count == 0)
            return;

        _assets.FarmAnimalCloneBatchApplier.Apply(
            asset: asset,
            clonedAnimals: _assets.ClonedAnimalsInfoList,
            loadedContentPacks: _assets.LoadedContentPacks
        );
    }
}
