using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;
using WildEggsHatching.Infrastructure.Content;

namespace WildEggsHatching.Infrastructure.Content.Editors;

public sealed class ObjectsEditor : AssetEditorBase
{
    public override string AssetName => GameAssets.Objects;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        _ = asset.AsDictionary<string, ObjectData>().Data;
        context.Log("ObjectsEditor applied (Phase 0 wiring).");
    }
}
