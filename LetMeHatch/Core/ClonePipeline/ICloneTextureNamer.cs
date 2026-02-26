namespace LetMeHatch.Core.ClonePipeline;

/// <summary>
/// Generates stable asset keys for per-clone textures.
/// </summary>
public interface ICloneTextureNamer
{
    string GetCloneTextureAssetName(string cloneItemId);
}
