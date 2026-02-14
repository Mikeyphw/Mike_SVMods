using System.Collections.Generic;

namespace WildEggsHatching.Core.Models;

/// <summary>
/// Immutable record representing an animal definition.
/// Replaces the mutable WildAnimalData class.
/// </summary>
public sealed record AnimalDefinition(
    /// <summary>Unique identifier for the animal.</summary>
    string Id,

    /// <summary>Display name shown in UI.</summary>
    string DisplayName,

    /// <summary>Base spawn weight for weighted selection.</summary>
    int BaseSpawnWeight,

    /// <summary>Category of the animal (Vanilla, Magic, Dinosaur, etc.).</summary>
    AnimalCategory Category,

    /// <summary>Whether this animal produces large eggs.</summary>
    bool ProducesLargeEggs,

    /// <summary>Whether this is a magic/supernatural animal.</summary>
    bool IsMagic,

    /// <summary>The mod that added this animal (Vanilla for base game).</summary>
    string ModID,

    /// <summary>The internal name of the egg item that hatches into this animal.</summary>
    string? HatchEggItemId,

    /// <summary>Minimum progression step required to unlock this animal.</summary>
    ProgressionStep MinProgressionStep,

    /// <summary>Custom spawn rules for this animal (JSON configuration).</summary>
    string? CustomRules,

    /// <summary>Description shown when animal is discovered.</summary>
    string? Description,

    /// <summary>Icon sprite path for UI display.</summary>
    string? IconPath
)
{
    /// <summary>
    /// Creates an AnimalDefinition with simplified parameters.
    /// </summary>
    public AnimalDefinition(
        string id,
        string displayName,
        int baseSpawnWeight,
        AnimalCategory category = AnimalCategory.Standard,
        bool producesLargeEggs = false,
        bool isMagic = false,
        string modId = "Vanilla")
        : this(
            id,
            displayName,
            baseSpawnWeight,
            category,
            producesLargeEggs,
            isMagic,
            modId,
            null,
            ProgressionStep.PartI,
            null,
            null,
            null)
    {
    }

    /// <summary>
    /// Creates a copy of this definition with modified properties.
    /// Enables fluent-style configuration while maintaining immutability.
    /// </summary>
    public AnimalDefinition WithDisplayName(string newDisplayName)
        => this with { DisplayName = newDisplayName };

    public AnimalDefinition WithBaseSpawnWeight(int newWeight)
        => this with { BaseSpawnWeight = newWeight };

    public AnimalDefinition WithCategory(AnimalCategory newCategory)
        => this with { Category = newCategory };

    public AnimalDefinition WithLargeEggs(bool producesLarge)
        => this with { ProducesLargeEggs = producesLarge };

    public AnimalDefinition WithMagic(bool isMagical)
        => this with { IsMagic = isMagical };

    public AnimalDefinition WithMinProgressionStep(ProgressionStep step)
        => this with { MinProgressionStep = step };

    public AnimalDefinition WithHatchEgg(string eggItemId)
        => this with { HatchEggItemId = eggItemId };

    public AnimalDefinition WithDescription(string? description)
        => this with { Description = description };

    public AnimalDefinition WithIcon(string iconPath)
        => this with { IconPath = iconPath };

    /// <summary>
    /// Gets the egg type that contains this animal.
    /// </summary>
    public EnumsAndStrings.EggType GetEggType()
    {
        if (IsMagic && ProducesLargeEggs)
            return EnumsAndStrings.EggType.largeMagicWildEgg;
        
        if (IsMagic)
            return EnumsAndStrings.EggType.magicWildEgg;
        
        if (ProducesLargeEggs)
            return EnumsAndStrings.EggType.largeWildEgg;
        
        return EnumsAndStrings.EggType.wildEgg;
    }

    /// <summary>
    /// Checks if this animal is from the base game.
    /// </summary>
    public bool IsVanilla() => ModID == "Vanilla";

    /// <summary>
    /// Checks if this animal is modded content.
    /// </summary>
    public bool IsModded() => ModID != "Vanilla";

    /// <summary>
    /// Checks if this animal can spawn at the given progression step.
    /// </summary>
    public bool CanSpawnAt(ProgressionStep currentStep)
    {
        return currentStep >= MinProgressionStep;
    }

    /// <summary>
    /// Returns a formatted string representation.
    /// </summary>
    public override string ToString()
    {
        var magicStr = IsMagic ? " [MAGIC]" : "";
        var largeStr = ProducesLargeEggs ? " [LARGE]" : "";
        return $"{DisplayName} ({Id}){magicStr}{largeStr} - Weight: {BaseSpawnWeight}";
    }
}

/// <summary>
/// Categories of animals for grouping and filtering.
/// </summary>
public enum AnimalCategory
{
    /// <summary>Standard farm animals (chicken, duck, etc.).</summary>
    Standard,

    /// <summary>Dinosaurs and prehistoric creatures.</summary>
    Dinosaur,

    /// <summary>Magic and supernatural creatures.</summary>
    Magic,

    /// <summary>Legendary and mythic creatures.</summary>
    Legendary,

    /// <summary>Void creatures.</summary>
    Void,

    /// <summary>Hybrid or crossbred animals.</summary>
    Hybrid
}

