namespace LetMeHatch.Core.ClonePipeline;

/// <summary>
/// Builds a minimal object-data shape for cloned items (Stardew types are handled outside Core).
/// </summary>
public interface ICloneObjectDataBuilder
{
    CloneObjectData Build(CloneObjectDataRequest request);
}

public sealed record CloneObjectDataRequest(
    CloneSpec Spec,
    string DisplayName,
    string Description
);

public sealed record CloneObjectData(
    string Id,
    string Name,
    string Description,
    string TextureAssetName,
    int SpriteIndex
);
