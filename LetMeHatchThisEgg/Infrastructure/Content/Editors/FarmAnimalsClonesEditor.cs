using LetMeHatchThisEgg.GameAssetManagement;
using LetMeHatchThisEgg.Infrastructure.Cloning;
using StardewModdingAPI;
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

        var farmAnimals = _assets.FarmAnimalAssetAdapter.Wrap(asset);
        var resolver = new LoadedContentPackResolver(_assets.LoadedContentPacks);

        _assets.FarmAnimalCloneBatchApplier.Apply(
            farmAnimalsRepository: farmAnimals,
            clonedAnimals: _assets.ClonedAnimalsInfoList,
            contentPacks: resolver
        );
    }
}
