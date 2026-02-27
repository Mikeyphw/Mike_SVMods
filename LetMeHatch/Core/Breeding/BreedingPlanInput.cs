using System;
using System.Collections.Generic;
using LetMeHatch.Models;
using Mikew.NestCore.Framework.Random;
using StardewValley.GameData.FarmAnimals;

namespace LetMeHatch.Core.Breeding;

/// <summary>
/// Inputs for building a <see cref="BreedingPlan"/>.
/// </summary>
public sealed record BreedingPlanInput(
    DateOnly InGameDate,
    int Seed,
    IRandomProvider Rng,
    IReadOnlyDictionary<string, FarmAnimalData> FarmAnimals,
    IReadOnlyDictionary<string, BreedingDataFemale> Females,
    IReadOnlyDictionary<string, int> MaleCountsByAnimalKey);
