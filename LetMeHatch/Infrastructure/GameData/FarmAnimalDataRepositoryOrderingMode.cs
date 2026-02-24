namespace LetMeHatch.Infrastructure.GameData;

/// <summary>Determines how the FarmAnimals asset dictionary should be rebuilt to stabilize iteration order.</summary>
public enum FarmAnimalDataRepositoryOrderingMode
{
    /// <summary>Do not rebuild the dictionary order.</summary>
    None,

    /// <summary>Rebuild the dictionary sorting all keys by ordinal.</summary>
    SortAllKeysOrdinal,

    /// <summary>
    /// Preserve the original key order captured at repository creation, then append any new keys sorted by ordinal.
    /// </summary>
    PreserveOriginalThenSortNewOrdinal,
}
