using LetMeHatchThisEgg.Breeding;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;

using static LetMeHatchThisEgg.ModEntry;

namespace LetMeHatchThisEgg.Models
{
    public class BreedingDataMale
    {
        public float MaleChance { get; set; }
        public float BoostByMale { get; set; }
        public Dictionary<ItemType, string?> EggsIDs { get; set; }
        private Dictionary<ItemType, string?> FinalEggsIDs { get; set; }
        public bool Enabled { get; set; }
        public bool AdjustableMaleChances { get; set; }
        private int Quantity { get; set; } = 0;

        public BreedingDataMale()
        {
            MaleChance = 0.5f;
            BoostByMale = 0.5f;
            EggsIDs = new Dictionary<ItemType, string?>()
            {
                { ItemType.Normal, "" },
                { ItemType.Large, "" },
                { ItemType.FertilizedEgg, "" }
            };
            FinalEggsIDs = new(EggsIDs);
            Enabled = true;
            AdjustableMaleChances = true;
        }

        public void SetEggID(ItemType type, string? eggID)
        {
            FinalEggsIDs[type] = eggID;
        }

        public string? GetEggID(ItemType type)
        {
            return FinalEggsIDs[type];
        }

        public Dictionary<ItemType, string?> GetEggIDs()
        {
            return FinalEggsIDs;
        }

        public void SetEggIDs(Dictionary<ItemType, string?> eggIDs)
        {
            FinalEggsIDs = new(eggIDs);
        }

        public void SetQuantity(string maleKey)
        {
            Quantity = BreedingSystem.GetMaleQuantity(maleKey);
        }

        public int GetQuantity()
        {
            return Quantity;
        }
    }
}