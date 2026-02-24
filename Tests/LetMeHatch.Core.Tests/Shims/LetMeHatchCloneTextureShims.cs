namespace LetMeHatch.EnumsAndStrings;

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

namespace LetMeHatch.Models;

using System.Collections.Generic;
using static LetMeHatch.EnumsAndStrings.Enums;

public sealed class ClonedAnimalInfo
{
    public string FemaleKey { get; set; } = string.Empty;
    public Dictionary<TextureType, string> Textures { get; set; } = new();
}
