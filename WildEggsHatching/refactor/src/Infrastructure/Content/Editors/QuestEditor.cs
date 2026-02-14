using StardewModdingAPI.Events;
using StardewValley.GameData.Quests;

namespace WildEggsHatching.Infrastructure.Content.Editors;

public sealed class QuestEditor : AssetEditorBase
{
    public override string AssetName => GameAssets.Quests;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        _ = asset.AsDictionary<string, QuestData>().Data;
        context.Log("QuestEditor applied (Phase 0 wiring).");
    }
}
