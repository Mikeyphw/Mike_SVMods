using static LetMeHatchThisEgg.EnumsAndStrings.Enums;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings.ItemIds;

using static LetMeHatchThisEgg.ModEntry;

namespace LetMeHatchThisEgg.Framework
{
    public class SavedItemsData
    {
        public Dictionary<
            string,
            Dictionary<CloneItemType, Dictionary<ItemType, string>?>
        > AnimalItemRelation = new();
        public Dictionary<CloneItemType, List<string>> FoundItems;
        public List<string> AnimalKeys = new();

        public SavedItemsData()
        {
            FoundItems = new()
            {
                [CloneItemType.Egg] = new List<string>() { string.Empty },
                [CloneItemType.Milk] = new List<string>() { string.Empty },
                [CloneItemType.Misc] = new List<string>()
                {
                    string.Empty,
                    AnimalProducts.DuckFeather,
                    AnimalProducts.Wool,
                    AnimalProducts.RabbitFoot
                }
            };
            FoundItems[CloneItemType.Egg].AddRange(AnimalProducts.VanillaEggs);
            FoundItems[CloneItemType.Milk].AddRange(AnimalProducts.VanillaMilk);
        }

        public void ResetRelations(
            Dictionary<
                string,
                Dictionary<CloneItemType, Dictionary<ItemType, string>>
            > defaultItemRelations
        )
        {
            foreach (var (animalKey, relation) in AnimalItemRelation)
            {
                // Reset each clone item type's items to the default values
                foreach (var (cloneItemType, _) in relation)
                {
                    AnimalItemRelation[animalKey][cloneItemType] = new Dictionary<ItemType, string>(
                        defaultItemRelations[animalKey][cloneItemType]
                    );
                }
            }
        }
    }
}
