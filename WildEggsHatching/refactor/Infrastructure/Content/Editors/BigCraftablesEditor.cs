using StardewModdingAPI.Events;
using StardewValley.GameData.BigCraftables;

namespace WildEggsHatching.Infrastructure.Content.Editors;

public sealed class BigCraftablesEditor : AssetEditorBase
{
    public override string AssetName => GameAssets.BigCraftables;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        _ = asset.AsDictionary<string, BigCraftableData>().Data;
        context.Log("BigCraftablesEditor applied (Phase 0 wiring).");
    }
}
