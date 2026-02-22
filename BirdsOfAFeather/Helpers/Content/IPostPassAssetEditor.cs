using System.Collections.Generic;

namespace Mikew.BirdsOfAFeather.Helpers.Content
{
    /// <summary>
    /// Optional extension for <see cref="IAssetEditor{TContext}"/> which allows registering one or more extra
    /// edit phases for the same asset request (e.g. Late, Late+100).
    /// </summary>
    public interface IPostPassAssetEditor<TContext> : IAssetEditor<TContext>
    {
        /// <summary>
        /// Ordered list of post-pass edits to register. Each pass is registered as a separate <c>e.Edit</c>.
        /// </summary>
        IReadOnlyList<AssetPostPass<TContext>> PostPasses { get; }
    }
}
