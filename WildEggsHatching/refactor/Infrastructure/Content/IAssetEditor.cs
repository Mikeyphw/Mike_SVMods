using StardewModdingAPI.Events;

namespace WildEggsHatching.Infrastructure.Content;

/// <summary>
/// Defines a content editor that can modify game assets.
/// This is the infrastructure layer contract - editors should be simple data transformers.
/// </summary>
public interface IAssetEditor
{
    /// <summary>The asset name this editor handles (e.g., "Data/Objects").</summary>
    string AssetName { get; }

    /// <summary>The edit priority for this editor.</summary>
    AssetEditPriority Priority { get; }

    /// <summary>
    /// Edits the asset data.
    /// </summary>
    /// <typeparam name="T">The asset data type.</typeparam>
    /// <param name="asset">The asset data to modify.</param>
    /// <param name="assetName">The asset name being edited.</param>
    void Edit<T>(IAssetData<T> asset, string assetName) where T : class;
}
