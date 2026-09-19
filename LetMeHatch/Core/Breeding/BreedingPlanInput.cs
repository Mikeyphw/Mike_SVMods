using System.Collections.Generic;
using LetMeHatch.Config;
using LetMeHatch.Models;
using Mikew.NestCore.Framework.Random;

namespace LetMeHatch.Core.Breeding;

/// <summary>
/// Inputs for building a <see cref="BreedingPlan"/> for a single female animal key.
/// </summary>
public sealed record BreedingPlanInput(
    DateOnly InGameDate,
    int Seed,
    IRandomProvider Rng,
    bool OnlyFertilizedEggs,
    float DailyLuckBonus,
    IReadOnlyCollection<string> ScopeAnimalKeys,
    string FemaleKey,
    IReadOnlyList<(string ContentPackId, BreedingDataFemale BreedingData)> Candidates,
    IReadOnlyDictionary<string, Dictionary<string, BreedingConfig>> BreedingConfigsByPack);
