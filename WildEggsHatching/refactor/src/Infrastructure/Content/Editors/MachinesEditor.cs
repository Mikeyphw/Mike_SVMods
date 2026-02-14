using StardewModdingAPI.Events;
using StardewValley.GameData.Machines;

namespace WildEggsHatching.Infrastructure.Content.Editors;

public sealed class MachinesEditor : AssetEditorBase
{
    public override string AssetName => GameAssets.Machines;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        _ = asset.AsDictionary<string, MachineData>().Data;
        context.Log("MachinesEditor applied (Phase 0 wiring).");
    }
}
