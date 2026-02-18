using StardewModdingAPI.Events;

namespace Mikew.BirdsOfAFeather.Helpers.Content
{
    /// <summary>
    /// Base class for <see cref="IAssetEditor{TContext}"/>.
    /// </summary>
    public abstract class AssetEditorBase<TContext> : IAssetEditor<TContext>
    {
        public abstract string AssetName { get; }
        public virtual AssetEditPriority Priority => AssetEditPriority.Normal;
        public abstract void Edit(IAssetData asset, TContext context);
    }
}
