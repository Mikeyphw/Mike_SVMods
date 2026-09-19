using System.Collections.Generic;
using LetMeHatch.Models;
using StardewValley.GameData.FarmAnimals;

namespace LetMeHatch.Infrastructure.Cloning;

/// <summary>Abstraction over clone purchase mapping rules for orchestration testing.</summary>
public interface ICloneMappingService
{
    void ApplyAlternatePurchaseRules(
        ClonedAnimalInfo clonedAnimalInfo,
        IDictionary<string, FarmAnimalData> farmAnimalsData
    );
}
