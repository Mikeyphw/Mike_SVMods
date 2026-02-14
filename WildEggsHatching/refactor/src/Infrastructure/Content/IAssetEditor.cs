using StardewModdingAPI.Events;

namespace WildEggsHatching.Infrastructure.Content;

public interface IAssetEditor
{
    string AssetName { get; }
    AssetEditPriority Priority { get; }

    void Edit(IAssetData asset, ContentContext context);
}
