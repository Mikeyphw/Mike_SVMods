using StardewModdingAPI.Events;

namespace LetMeHatchThisEgg.Infrastructure.Content;

public abstract class AssetEditorBase : IAssetEditor, IContentHandlerDebugInfo
{
    public abstract string AssetName { get; }
    public virtual AssetEditPriority Priority => AssetEditPriority.Default;

    public virtual string DebugHandles => AssetName;

    public virtual string DebugGates => "Runs when requested asset matches; may early-return based on config and asset state.";

    public abstract void Edit(IAssetData asset, ContentContext context);
}
