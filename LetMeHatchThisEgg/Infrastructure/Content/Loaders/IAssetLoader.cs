using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace LetMeHatchThisEgg.Infrastructure.Content.Loaders;

/// <summary>Loads (or overrides) a requested asset via SMAPI load hooks (e.g., LoadFromModFile).</summary>
public interface IAssetLoader
{
    /// <summary>Load priority to use when calling SMAPI load APIs (if needed).</summary>
    AssetLoadPriority Priority { get; }

    /// <summary>Whether this loader handles the requested asset.</summary>
    bool CanLoad(string assetNameLower, AssetRequestedEventArgs e);

    /// <summary>Perform the load/edit operations for this asset request.</summary>
    void Load(string assetNameLower, AssetRequestedEventArgs e);
}
