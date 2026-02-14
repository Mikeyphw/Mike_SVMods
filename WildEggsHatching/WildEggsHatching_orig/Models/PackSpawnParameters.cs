using BirdsOfAFeather.Helpers;
using Microsoft.Xna.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using WildEggsHatching.Services;
using static WildEggsHatching.EnumsAndStrings.Enums;

namespace WildEggsHatching.Models
{
    public class PackSpawnParameters
    {
        public int? MinEggArea;
        public int? MaxEggArea;
        public int? MaxEggPack;
        public int CertainEggs;
        public bool UseJsonValues;
        public int MaxEggs_All;

        public PackSpawnParameters()
        {
            // Default constructor with no defaults set
        }

        public PackSpawnParameters(
            int? minEggArea,
            int? maxEggArea,
            int? maxEggPack,
            int certainEggs,
            bool useJsonValues,
            int maxEggs_All
        )
        {
            MinEggArea = minEggArea;
            MaxEggArea = maxEggArea;
            MaxEggPack = maxEggPack;
            CertainEggs = certainEggs;
            UseJsonValues = useJsonValues;
            MaxEggs_All = maxEggs_All;
        }

        public int MinEggs => CertainEggs;
    }
}
