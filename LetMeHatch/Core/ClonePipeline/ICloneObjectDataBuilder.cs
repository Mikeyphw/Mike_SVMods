using System.Collections.Generic;

namespace LetMeHatch.Core.ClonePipeline;

/// <summary>
/// Builds a minimal object-data blueprint for cloned items using a whitelist of fields.
/// Stardew-specific <c>ObjectData</c> mapping happens outside Core.
/// </summary>
public interface ICloneObjectDataBuilder
{
    CloneObjectData Build(CloneObjectDataRequest request);
}

/// <summary>
/// Source snapshot of an object used as the copy baseline.
/// </summary>
public sealed record CloneObjectDataSource(
    string Name,
    string Description,
    int Price,
    int Edibility,
    int Category,
    IReadOnlyList<string>? ContextTags = null,
    IReadOnlyDictionary<string, string>? CustomFields = null,
    string? TextureAssetName = null,
    int SpriteIndex = 0
);

/// <summary>
/// Overrides applied on top of <see cref="CloneObjectDataSource"/>.
/// </summary>
public sealed record CloneObjectDataOverrides(
    string? Name = null,
    string? Description = null,
    string? TextureAssetName = null,
    int? SpriteIndex = null,
    IReadOnlyList<string>? AdditionalContextTags = null,
    IReadOnlyDictionary<string, string>? AdditionalCustomFields = null
);

public sealed record CloneObjectDataRequest(
    CloneSpec Spec,
    CloneObjectDataSource Source,
    CloneObjectDataOverrides? Overrides = null
);

/// <summary>
/// Minimal blueprint to later map into Stardew <c>ObjectData</c>.
/// </summary>
public sealed record CloneObjectData(
    string Id,
    string Name,
    string Description,
    int Price,
    int Edibility,
    int Category,
    IReadOnlyList<string> ContextTags,
    IReadOnlyDictionary<string, string> CustomFields,
    string TextureAssetName,
    int SpriteIndex
);
