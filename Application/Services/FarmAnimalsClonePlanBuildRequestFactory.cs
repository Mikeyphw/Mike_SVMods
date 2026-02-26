using System;
using System.Collections.Generic;
using System.Linq;
using LetMeHatch.Application.Gateways;
using LetMeHatch.Application.UseCases;
using LetMeHatch.Core.ClonePipeline;
using StardewValley.GameData.FarmAnimals;

namespace LetMeHatch.Application.Services;

/// <summary>
/// Creates <see cref="ClonePlanBuildRequest"/> by reading <c>Data/FarmAnimals</c>.
/// </summary>
public sealed class FarmAnimalsClonePlanBuildRequestFactory : IClonePlanBuildRequestFactory
{
    private readonly IGameDataGateway _gameData;

    public FarmAnimalsClonePlanBuildRequestFactory(IGameDataGateway gameData)
    {
        _gameData = gameData ?? throw new ArgumentNullException(nameof(gameData));
    }

    public ClonePlanBuildRequest Create(string modUniqueId)
    {
        if (string.IsNullOrWhiteSpace(modUniqueId))
            throw new ArgumentException("Mod unique id must be non-empty.", nameof(modUniqueId));

        var animals = _gameData.Load<Dictionary<string, FarmAnimalData>>("Data/FarmAnimals");

        var snapshot = animals.ToDictionary(
            static kvp => kvp.Key,
            static kvp => new FarmAnimalProduceLists(
                ProduceItemIds: (kvp.Value.ProduceItemIds ?? new List<FarmAnimalProduce>())
                    .Select(static p => p.ItemId)
                    .Where(static s => !string.IsNullOrWhiteSpace(s))
                    .ToList(),
                DeluxeProduceItemIds: (kvp.Value.DeluxeProduceItemIds ?? new List<FarmAnimalProduce>())
                    .Select(static p => p.ItemId)
                    .Where(static s => !string.IsNullOrWhiteSpace(s))
                    .ToList()
            ),
            StringComparer.OrdinalIgnoreCase
        );

        return new ClonePlanBuildRequest(modUniqueId, snapshot);
    }
}
