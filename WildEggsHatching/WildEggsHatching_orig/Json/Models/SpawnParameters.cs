using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using static WildEggsHatching.EnumsAndStrings.Enums;

namespace WildEggsHatching.Models
{
    public class SpawnParameters
    {
        public int MaxSpawningTries = 400;
        public int MaxEggPerMap_Any = 3;
        public int MaxEggPerMap_Magic = 1;
        public int MaxEggPerMap_Large = 2;
        public float BaseRandomChance = 0.15f;
        public Dictionary<EggType, int> MaxWorldEggs;
        public PackSpawnParameters Parameters_Packs;
        public PackSpawnParameters Parameters_Vanilla;

        public SpawnParameters()
        {
            MaxWorldEggs = new Dictionary<EggType, int>
            {
                { EggType.wildEgg, 99 },
                { EggType.largeWildEgg, 10 },
                { EggType.magicWildEgg, 3 },
                { EggType.largeMagicWildEgg, 2 },
            };

            Parameters_Packs = new PackSpawnParameters(
                minEggArea: 1,
                maxEggArea: 1,
                maxEggPack: 3,
                certainEggs: 1,
                useJsonValues: true,
                maxEggs_All: 5
            );

            Parameters_Vanilla = new PackSpawnParameters(
                minEggArea: 1,
                maxEggArea: 2,
                maxEggPack: 5,
                certainEggs: 2,
                useJsonValues: true,
                maxEggs_All: 5
            );
        }
    }
}
