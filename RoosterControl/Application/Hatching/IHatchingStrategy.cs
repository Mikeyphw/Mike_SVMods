using System.Collections.Generic;
using StardewValley.GameData.FarmAnimals;

namespace RoosterControl.Application.Hatching;

internal interface IHatchingStrategy
{
    void Apply(
        HatchingContext ctx,
        string femaleKey,
        RoosterData rooster,
        IDictionary<string, FarmAnimalData> data,
        IReadOnlyList<string> otherAnimalsSharingEggs
    );
}
