using LetMeHatch.Infrastructure.Content;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace LetMeHatch.Infrastructure.Content.Loaders;

/// <summary>Convenience base for asset loaders.</summary>
public abstract class AssetLoaderBase : IAssetLoader, IContentHandlerDebugInfo
{
    protected AssetLoaderBase(AssetLoadPriority priority)
    {
        Priority = priority;
    }

    public AssetLoadPriority Priority { get; }

    public virtual string DebugHandles => "See CanLoad(assetNameLower, e).";

    public virtual string DebugGates => "Runs when CanLoad returns true; may be exclusive.";

    public abstract bool CanLoad(string assetNameLower, AssetRequestedEventArgs e);

    public abstract void Load(string assetNameLower, AssetRequestedEventArgs e);
}
