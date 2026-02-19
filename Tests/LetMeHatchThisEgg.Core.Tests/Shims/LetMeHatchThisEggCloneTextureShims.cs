namespace LetMeHatchThisEgg.EnumsAndStrings;

public static class Enums
{
    public enum TextureType
    {
        Adult,
        Baby,
        Harvested,
        Shop
    }
}

namespace LetMeHatchThisEgg.Models;

using System.Collections.Generic;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;

public sealed class ClonedAnimalInfo
{
    public string FemaleKey { get; set; } = string.Empty;
    public Dictionary<TextureType, string> Textures { get; set; } = new();
}
