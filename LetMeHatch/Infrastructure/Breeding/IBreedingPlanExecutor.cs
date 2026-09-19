using System.Collections.Generic;
using LetMeHatch.Core.Breeding;
using StardewValley.GameData.FarmAnimals;

namespace LetMeHatch.Infrastructure.Breeding;

/// <summary>
/// Applies a <see cref="BreedingPlan"/> to the provided game-data map.
/// </summary>
public interface IBreedingPlanExecutor
{
    void Apply(IDictionary<string, FarmAnimalData> farmAnimals, BreedingPlan plan);
}
