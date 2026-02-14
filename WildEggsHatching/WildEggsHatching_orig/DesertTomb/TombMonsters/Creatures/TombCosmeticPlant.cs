using StardewValley;
using StardewValley.Extensions;
using StardewValley.TerrainFeatures;
using static Mikew.BirdsOfAFeather.Helpers.ModAssetHandler;
using static WildEggsHatching.EnumsAndStrings.Enums.Assets;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.DesertTomb.TombMonsters.Creatures
{
    internal class TombCosmeticPlant : CosmeticPlant
    {
        internal TombCosmeticPlant(int which)
            : base(which)
        {
            flipped.Value = Game1.random.NextBool();
        }

        public override string textureName()
        {
            return AssetHandler.GetAssetPath(Misc.Plants, AssetPathType.GamePath);
        }
    }
}
