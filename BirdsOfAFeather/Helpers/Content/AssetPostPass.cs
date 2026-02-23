using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Mikew.BirdsOfAFeather.Helpers.Content
{
    /// <summary>
    /// Defines an additional edit pass for an asset, registered as a separate <c>e.Edit(...)</c> call.
    /// Use a very-late <see cref="AssetEditPriority"/> value to emulate "Late+100" style compatibility layers.
    /// </summary>
    public sealed class AssetPostPass<TContext>
    {
        public AssetPostPass(string debugName, AssetEditPriority priority, Action<IAssetData, TContext> apply)
        {
            DebugName = string.IsNullOrWhiteSpace(debugName) ? "PostPass" : debugName;
            Priority = priority;
            Apply = apply ?? throw new ArgumentNullException(nameof(apply));
        }

        public string DebugName { get; }
        public AssetEditPriority Priority { get; }
        public Action<IAssetData, TContext> Apply { get; }
    }
}
