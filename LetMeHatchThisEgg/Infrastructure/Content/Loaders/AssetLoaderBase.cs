using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace LetMeHatchThisEgg.Infrastructure.Content.Loaders;

/// <summary>Convenience base for asset loaders.</summary>
public abstract class AssetLoaderBase : IAssetLoader
{
    protected AssetLoaderBase(AssetLoadPriority priority)
    {
        Priority = priority;
    }

    public AssetLoadPriority Priority { get; }

    public abstract bool CanLoad(string assetNameLower, AssetRequestedEventArgs e);

    public abstract void Load(string assetNameLower, AssetRequestedEventArgs e);
}
