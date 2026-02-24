using System;
using LetMeHatch.EnumsAndStrings;

namespace LetMeHatch.Core.Cloning;

/// <summary>
/// Resolves clone texture fields. Keeps the decision rules in Core while callers handle asset loading/caching.
/// </summary>
public static class CloneTextureResolver
{
    public sealed record Resolution(string Value, bool IsContentPackAsset);

    public static Resolution Resolve(
        string? currentValue,
        string? originalValue,
        string contentPackId,
        string femaleKey,
        Enums.TextureType textureType
    )
    {
        if (string.IsNullOrWhiteSpace(contentPackId))
        {
            throw new ArgumentException("Content pack id must be provided.", nameof(contentPackId));
        }

        if (string.IsNullOrWhiteSpace(femaleKey))
        {
            throw new ArgumentException("Female key must be provided.", nameof(femaleKey));
        }

        if (currentValue is null)
        {
            return new(originalValue ?? string.Empty, false);
        }

        if (currentValue.Equals("cp", StringComparison.OrdinalIgnoreCase))
        {
            var path = BuildContentPackTexturePath(contentPackId, femaleKey, textureType);
            return new(path, true);
        }

        return new((string)currentValue.Clone(), false);
    }

    public static string BuildContentPackTexturePath(
        string contentPackId,
        string femaleKey,
        Enums.TextureType textureType
    )
    {
        return $"Animals/MaleClones/{contentPackId}/{femaleKey}/{textureType}";
    }
}
