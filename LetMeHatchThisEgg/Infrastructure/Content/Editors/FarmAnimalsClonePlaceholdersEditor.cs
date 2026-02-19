using System.Linq;
using LetMeHatchThisEgg.Cloning;
using LetMeHatchThisEgg.GameAssetManagement;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.FarmAnimals;

namespace LetMeHatchThisEgg.Infrastructure.Content.Editors;

/// <summary>Adds placeholder entries for male clone animals before later edits fill them in.</summary>
public sealed class FarmAnimalsClonePlaceholdersEditor : AssetEditorBase
{
    private readonly GameAssetManager _assets;

    public FarmAnimalsClonePlaceholdersEditor(GameAssetManager assets)
    {
        _assets = assets;
    }

    public override string AssetName => GameAssets.FarmAnimals;

    public override AssetEditPriority Priority => AssetEditPriority.Early;

        public override string DebugGates => "ClonedAnimalsInfoList.Any().";

public override void Edit(IAssetData asset, ContentContext context)
    {
        if (!_assets.ClonedAnimalsInfoList.Any())
            return;

        var data = asset.AsDictionary<string, FarmAnimalData>().Data;
        foreach (var maleKey in _assets.ClonedAnimalsInfoList.Select(x => x.MaleKey))
        {
            data[maleKey] = AnimalCloner.GetFarmAnimalDataPlaceholder();
            context.Log.Log($"Added {maleKey} placeholder", LogLevel.Trace);
        }
    }
}
