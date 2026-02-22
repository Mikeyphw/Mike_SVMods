using StardewModdingAPI.Events;

namespace Mikew.BirdsOfAFeather.Helpers.Content
{
    /// <summary>
    /// Optional second-pass editor hook that runs after the primary edit phase.
    /// Use this to apply compatibility fixes similar to a "Late+N" edit layer.
    /// </summary>
    public interface IPostPassAssetEditor<in TContext> : IAssetEditor<TContext>
    {
        /// <summary>
        /// The priority to use for the post-pass edit. Choose a late priority to run after most edits.
        /// </summary>
        AssetEditPriority PostPriority { get; }

        /// <summary>
        /// Apply the post-pass edit.
        /// </summary>
        void EditPost(IAssetData asset, TContext context);
    }
}
