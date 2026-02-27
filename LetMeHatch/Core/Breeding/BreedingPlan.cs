using System;
using System.Collections.Generic;

namespace LetMeHatch.Core.Breeding;

/// <summary>
/// Pure-data plan describing which breeding decisions were made and which mutations should be applied.
/// </summary>
public sealed record BreedingPlan(
    string PlanId,
    DateOnly InGameDate,
    int Seed,
    IReadOnlyCollection<string> ScopeAnimalKeys,
    IReadOnlyList<BreedingPairPlan> Pairs,
    IReadOnlyList<BreedingMutation> Mutations,
    IReadOnlyList<string> Diagnostics);

/// <summary>
/// Breeding decision for a single female animal key.
/// </summary>
public sealed record BreedingPairPlan(
    string FemaleAnimalKey,
    string MaleAnimalKey,
    IReadOnlyList<string> OtherMaleKeys,
    BreedingMode Mode,
    string? EggToClearItemId,
    string? EggToAssignItemId,
    bool FertilizedSucceeded,
    int MaleQuantity,
    double BaseFertilizedChance,
    double AdjustedFertilizedChance,
    double MaleChance,
    double BoostByMale,
    bool ApplyDailyLuckBoost,
    string? SelectedEggType);

/// <summary>
/// How egg generation should behave for the pair.
/// </summary>
public enum BreedingMode
{
    /// <summary>
    /// Normal (non-fertilized) egg selection.
    /// </summary>
    Normal,

    /// <summary>
    /// Fertilized egg selection gated by fertilization chance.
    /// </summary>
    Fertilized
}

/// <summary>
/// Mutation operations the executor can apply to farm animal game data.
/// </summary>
public abstract record BreedingMutation
{
    /// <summary>
    /// Ensure the cloned male animal key exists in the game-data dictionary.
    /// </summary>
    public sealed record EnsureCloneExists(string OriginalKey, string CloneKey) : BreedingMutation;

    /// <summary>
    /// Canonical (destructive-by-design) rebuild of the egg-domain for a female animal type.
    /// </summary>
    public sealed record RebuildFemaleEggDomain(
        string FemaleKey,
        IReadOnlyList<string> CanonicalEggItemIds,
        string? FertilizedEggItemId) : BreedingMutation;

    /// <summary>
    /// Hard safety rule. Executor must enforce this as last-write-wins.
    /// </summary>
    public sealed record EnforceMaleNeverLays(string MaleKey) : BreedingMutation;

    /// <summary>
    /// Remove egg-like entries from produce lists (non-egg produces must be preserved).
    /// </summary>
    public sealed record StripEggLikeFromProduces(string AnimalKey) : BreedingMutation;
}
