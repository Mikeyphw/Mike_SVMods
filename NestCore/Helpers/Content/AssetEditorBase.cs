using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Mikew.NestCore.Helpers.Content
{
    /// <summary>
    /// Base class for <see cref="IAssetEditor{TContext}"/>.
    /// </summary>
    public abstract class AssetEditorBase<TContext> : IAssetEditor<TContext>
    {
        public abstract string AssetName { get; }
        public virtual AssetEditPriority Priority => AssetEditPriority.Default;
        public abstract void Edit(IAssetData asset, TContext context);
    }
}
