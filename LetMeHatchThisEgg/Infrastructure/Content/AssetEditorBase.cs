using StardewModdingAPI.Events;

namespace LetMeHatchThisEgg.Infrastructure.Content;

public abstract class AssetEditorBase : IAssetEditor
{
    public abstract string AssetName { get; }
    public virtual AssetEditPriority Priority => AssetEditPriority.Default;

    public abstract void Edit(IAssetData asset, ContentContext context);
}
