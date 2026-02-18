using StardewValley.GameData.FarmAnimals;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;

using static LetMeHatchThisEgg.ModEntry; namespace LetMeHatchThisEgg.Models
{
    public class ClonedAnimalInfo
    {
        public string ContentPackID;
        public string FemaleKey;
        public string MaleKey;
        public Dictionary<TextureType, string> Textures;
        public FarmAnimalData AnimalData;
        public bool AddAsAlternatePurchase;

        public ClonedAnimalInfo()
        {
            ContentPackID = string.Empty;
            FemaleKey = string.Empty;
            MaleKey = string.Empty;
            AnimalData = new();
            Textures = new();
            AddAsAlternatePurchase = true;
        }
    }

}