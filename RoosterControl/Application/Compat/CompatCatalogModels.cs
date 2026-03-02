using System.Collections.Generic;

namespace RoosterControl.Application.Compat;

/// <summary>
/// Logical egg roles RoosterControl can select from multiple sources.
/// </summary>
public enum EggKind
{
    BrownEgg,
    LargeBrownEgg,
    WhiteEgg,
    LargeWhiteEgg,

    BlueEgg,
    LargeBlueEgg,

    VoidEgg,
    LargeVoidEgg,

    GoldEgg,
    LargeGoldEgg,

    FertilizedBrownEgg,
    FertilizedWhiteEgg,
    FertilizedBlueEgg,
    FertilizedVoidEgg,
    FertilizedGoldEgg,

    RoosterEgg
}

/// <summary>
/// Where an egg item should be sourced from.
/// </summary>
public enum EggSourceRefList
{
    EggItemIds,
    ProduceItemIds,
    DeluxeProduceItemIds
}

/// <summary>
/// Reference to an egg item ID stored on a <c>Data/FarmAnimals</c> entry.
/// </summary>
public sealed record FarmAnimalEggRef(string FarmAnimalKey, EggSourceRefList List, int Index);

/// <summary>
/// Defines a selectable egg source.
/// Exactly one of <see cref="ItemId"/> or <see cref="FarmAnimalRef"/> should be set.
/// </summary>
public sealed record EggSourceDef
{
    public required string SourceId { get; init; }
    public required string DisplayName { get; init; }
    public required EggKind EggKind { get; init; }

    public string? ItemId { get; init; }
    public FarmAnimalEggRef? FarmAnimalRef { get; init; }
}

/// <summary>
/// Breeding behavior for a farm animal key.
/// </summary>
public enum BreedingRole
{
    Male,
    Female,
    Both,
    None
}

/// <summary>
/// Whether an animal should be treated as an egg-layer.
/// </summary>
public enum EggLayerRole
{
    /// <summary>Respect what's actually present in the <c>Data/FarmAnimals</c> entry.</summary>
    InheritFromFarmAnimalData,

    /// <summary>Force egg-layer behavior even if the entry doesn't include eggs.</summary>
    LaysEggs,

    /// <summary>Force non-egg-layer behavior even if the entry includes eggs.</summary>
    NoEggs
}

/// <summary>
/// A specific <c>Data/FarmAnimals</c> key owned by a rooster provider.
/// </summary>
public sealed record RoosterFarmAnimalKeyDef
{
    public required string Key { get; init; }
    public required BreedingRole BreedingRole { get; init; }
    public EggLayerRole EggLayerRole { get; init; } = EggLayerRole.InheritFromFarmAnimalData;
}

/// <summary>
/// Defines a selectable rooster source (provider).
/// </summary>
public sealed record RoosterSourceDef
{
    public required string SourceId { get; init; }
    public required string DisplayName { get; init; }

    public required List<RoosterFarmAnimalKeyDef> FarmAnimalKeys { get; init; }

    public string? RoosterEggItemId { get; init; }
}

/// <summary>
/// CP-patchable egg source catalog.
/// </summary>
public sealed record EggSourcesCatalog
{
    public int SchemaVersion { get; init; } = 1;
    public required List<EggSourceDef> Sources { get; init; }
}

/// <summary>
/// CP-patchable rooster source catalog.
/// </summary>
public sealed record RoosterSourcesCatalog
{
    public int SchemaVersion { get; init; } = 1;
    public required List<RoosterSourceDef> Sources { get; init; }
}
