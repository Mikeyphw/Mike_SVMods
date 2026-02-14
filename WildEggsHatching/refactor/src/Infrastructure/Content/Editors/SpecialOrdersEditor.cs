using StardewModdingAPI.Events;
using StardewValley.GameData.SpecialOrders;

namespace WildEggsHatching.Infrastructure.Content.Editors;

public sealed class SpecialOrdersEditor : AssetEditorBase
{
    public override string AssetName => GameAssets.SpecialOrders;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        _ = asset.AsDictionary<string, SpecialOrderData>().Data;
        context.Log("SpecialOrdersEditor applied (Phase 0 wiring).");
    }
}
