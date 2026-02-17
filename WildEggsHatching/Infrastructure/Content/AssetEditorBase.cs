using StardewModdingAPI.Events;

namespace WildEggsHatching.Infrastructure.Content;

public abstract class AssetEditorBase : IAssetEditor
{
    public abstract string AssetName { get; }
    public virtual AssetEditPriority Priority => AssetEditPriority.Normal;

    public abstract void Edit(IAssetData asset, ContentContext context);
}
