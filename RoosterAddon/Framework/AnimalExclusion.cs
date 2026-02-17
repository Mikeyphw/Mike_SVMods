using System.Collections.Generic;

namespace RoosterAddon.Framework;

/// <summary>
/// User-editable exclusion list loaded from <c>data/exclusions.json</c>.
/// </summary>
public sealed class AnimalExclusion
{
    public List<string> AnimalExclusions { get; set; } = new()
    {
        "Domestic Guinea Fowl",
        "White Legbar",
        "Blue Parrot",
        "White Parrot",
        "Red Parrot",
        "Void Parrot",
        "Echidna",
        "Birch Echidna",
        "Knuckles the Echidna",
        "Oak Echidna",
        "Warm Echidna",
        "Lady Amherst's Pheasant",
        "ExampleAnimalKey1",
        "ExampleAnimalKey2",
        "ExampleAnimalKey3",
        "OR",
        "ExampleAnimalKey1, ExampleAnimalKey2, ExampleAnimalKey3"
    };

    public List<string> AnimalExclusionsModID { get; set; } = new() { "ModIDExample" };
}
