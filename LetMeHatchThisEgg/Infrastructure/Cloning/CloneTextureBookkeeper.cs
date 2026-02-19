using System;
using LetMeHatchThisEgg.Core.Cloning;
using LetMeHatchThisEgg.Models;
using StardewValley.GameData.FarmAnimals;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;

namespace LetMeHatchThisEgg.Infrastructure.Cloning;

/// <summary>
/// Resolves clone texture fields and records any content-pack texture paths onto clone metadata.
/// </summary>
public sealed class CloneTextureBookkeeper : ICloneTextureBookkeeper
{
    public void ApplyTextures(
        string contentPackId,
        ClonedAnimalInfo cloneInfo,
        FarmAnimalData cloneData,
        FarmAnimalData originalData,
        FarmAnimalData placeholderData
    )
    {
        if (string.IsNullOrWhiteSpace(contentPackId))
        {
            throw new ArgumentException("Content pack id must be provided.", nameof(contentPackId));
        }

        if (cloneInfo is null)
        {
            throw new ArgumentNullException(nameof(cloneInfo));
        }

        if (cloneData is null)
        {
            throw new ArgumentNullException(nameof(cloneData));
        }

        if (originalData is null)
        {
            throw new ArgumentNullException(nameof(originalData));
        }

        if (placeholderData is null)
        {
            throw new ArgumentNullException(nameof(placeholderData));
        }

        cloneData.BabyTexture = ResolveAndRecord(
            currentValue: cloneData.BabyTexture,
            originalValue: originalData.BabyTexture,
            contentPackId: contentPackId,
            femaleKey: cloneInfo.FemaleKey,
            cloneInfo: cloneInfo,
            textureType: TextureType.Baby
        );

        cloneData.Texture = ResolveAndRecord(
            currentValue: cloneData.Texture,
            originalValue: originalData.Texture,
            contentPackId: contentPackId,
            femaleKey: cloneInfo.FemaleKey,
            cloneInfo: cloneInfo,
            textureType: TextureType.Adult
        );

        cloneData.HarvestedTexture = ResolveAndRecord(
            currentValue: cloneData.HarvestedTexture,
            originalValue: originalData.HarvestedTexture,
            contentPackId: contentPackId,
            femaleKey: cloneInfo.FemaleKey,
            cloneInfo: cloneInfo,
            textureType: TextureType.Harvested
        );

        cloneData.ShopTexture = ResolveAndRecord(
            currentValue: cloneData.ShopTexture,
            originalValue: originalData.ShopTexture,
            contentPackId: contentPackId,
            femaleKey: cloneInfo.FemaleKey,
            cloneInfo: cloneInfo,
            textureType: TextureType.Shop
        );
    }

    private static string ResolveAndRecord(
        string? currentValue,
        string originalValue,
        string contentPackId,
        string femaleKey,
        ClonedAnimalInfo cloneInfo,
        TextureType textureType
    )
    {
        var result = CloneTextureResolver.Resolve(
            currentValue: currentValue,
            originalValue: originalValue,
            contentPackId: contentPackId,
            femaleKey: femaleKey,
            textureType: textureType
        );

        if (result.IsContentPackAsset)
        {
            cloneInfo.Textures[textureType] = result.Value;
        }

        return result.Value;
    }
}
