using StardewModdingAPI.Events;
using StardewValley.GameData.FarmAnimals;

namespace WildEggsHatching.Infrastructure.Content.Editors;

public sealed class FarmAnimalsEditor : AssetEditorBase
{
    public override string AssetName => GameAssets.FarmAnimals;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        _ = asset.AsDictionary<string, FarmAnimalData>().Data;
        context.Log("FarmAnimalsEditor applied (Phase 0 wiring).");
    }
}
