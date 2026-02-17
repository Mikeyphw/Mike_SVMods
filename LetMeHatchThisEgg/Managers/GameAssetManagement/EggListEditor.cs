using System.Text.RegularExpressions;
using LetMeHatchThisEgg.Config;
using LetMeHatchThisEgg.Helpers;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;
using Object = StardewValley.Object;

using static LetMeHatchThisEgg.ModEntry;

namespace LetMeHatchThisEgg.GameAssetManagement
{
    public class EggListEditor 
    {
        private readonly CloningHelper _cloningHelper;

        public EggListEditor( CloningHelper cloningHelper)
        {
            _cloningHelper = cloningHelper;
        }

        public void SetUpEggList(IDictionary<string, FarmAnimalData> data)
        {
            var exclusions = GetExclusions();
            ModEntry.Config.EditedAnimals[EditType.EggList] = new();

            foreach (var (key, animal) in data)
            {
                var eggsItems = SearchProduces(
                    animal.ProduceItemIds.Concat(animal.DeluxeProduceItemIds).ToList()
                );

                if (animal.EggItemIds != null)
                {
                    RemoveClonedEggs(animal, eggsItems);
                    UpdateEggItemIds(animal, eggsItems, key, exclusions);
                }
                else if (animal.EggItemIds == null && eggsItems.Any())
                {
                    animal.EggItemIds = eggsItems;
                    TryAddEditedAnimal(key, exclusions);
                }
            }
        }

        private List<string> GetExclusions()
        {
            return ModEntry.Config
                .EditedAnimals[EditType.EggList]
                .Where(a => !a.Value)
                .Select(a => a.Key)
                .ToList();
        }

        private void RemoveClonedEggs(FarmAnimalData animal, List<string> eggsItems)
        {
            foreach (var egg in eggsItems.Where(egg => CloningHelper.IsClonedEgg(egg)))
            {
                var originalEgg = _cloningHelper.GetOriginalItemAndAnimalID(egg).OriginalItemID;
                animal.EggItemIds.RemoveAll(e => e == originalEgg);
            }
        }

        private void UpdateEggItemIds(
            FarmAnimalData animal,
            List<string> eggsItems,
            string key,
            List<string> exclusions
        )
        {
            var oldList = new List<string>(animal.EggItemIds);
            animal.EggItemIds.AddRange(eggsItems.Except(animal.EggItemIds));

            if (
                !oldList.OrderBy(x => x).SequenceEqual(animal.EggItemIds.OrderBy(x => x))
                && !exclusions.Contains(key)
            )
            {
                TryAddEditedAnimal(key, exclusions);
            }
        }

        private void TryAddEditedAnimal(string key, List<string> exclusions)
        {
            if (!exclusions.Contains(key))
            {
                ModEntry.Config.EditedAnimals[EditType.EggList].TryAdd(key, true);
            }
        }

        public List<string> SearchProduces(List<FarmAnimalProduce> animalProduces)
        {
            var eggsItems = new List<string>();
            if (animalProduces != null)
            {
                var pattern = @".*egg.*";
                var foundEggs = animalProduces
                    .Select(p => p.ItemId)
                    .Where(pid =>
                        !string.IsNullOrEmpty(pid)
                        && CheckIfIsAValidEgg(pid, pattern, Game1.objectData)
                    );

                eggsItems.AddRange(foundEggs);
            }
            return eggsItems;
        }

        private bool CheckIfIsAValidEgg(
            string? itemID,
            string pattern,
            IDictionary<string, ObjectData> gameObjectData
        )
        {
            if (string.IsNullOrEmpty(itemID))
            {
                LoggerHelper.VerboseLog("Item ID is null or empty, not finding valid egg");
                return false;
            }

            if (!gameObjectData.ContainsKey(itemID))
            {
                return CloningHelper.IsClonedEgg(itemID);
            }

            return Regex.IsMatch(itemID, pattern, RegexOptions.IgnoreCase)
                || (
                    gameObjectData[itemID].Category == Object.EggCategory
                    && Regex.IsMatch(
                        int.TryParse(itemID, out _) ? gameObjectData[itemID].DisplayName : itemID,
                        pattern,
                        RegexOptions.IgnoreCase
                    )
                );
        }
    }
}