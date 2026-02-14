using StardewModdingAPI.Events;

namespace WildEggsHatching.Infrastructure.Content.Editors;

public sealed class CraftingRecipesEditor : AssetEditorBase
{
    public override string AssetName => GameAssets.CraftingRecipes;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        _ = asset.AsDictionary<string, string>().Data;
        context.Log("CraftingRecipesEditor applied (Phase 0 wiring).");
    }
}
