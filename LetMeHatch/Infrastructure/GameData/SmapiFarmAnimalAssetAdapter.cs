using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley.GameData.FarmAnimals;

namespace LetMeHatch.Infrastructure.GameData;

/// <summary>SMAPI-backed adapter over the FarmAnimals asset dictionary.</summary>
public sealed class SmapiFarmAnimalAssetAdapter : IFarmAnimalAssetAdapter
{
    public IFarmAnimalDataRepository Wrap(IAssetData asset)
    {
        if (asset is null)
            throw new ArgumentNullException(nameof(asset));

        return new SmapiFarmAnimalDataRepository(asset);
    }

    private sealed class SmapiFarmAnimalDataRepository : IFarmAnimalDataRepository
    {
        private readonly IDictionary<string, FarmAnimalData> _data;

        public SmapiFarmAnimalDataRepository(IAssetData asset)
        {
            _data = asset.AsDictionary<string, FarmAnimalData>().Data;
        }

        public IDictionary<string, FarmAnimalData> Data => _data;

        public FarmAnimalData Get(string key) => _data[key];

        public void Set(string key, FarmAnimalData value) => _data[key] = value;
    }
}
