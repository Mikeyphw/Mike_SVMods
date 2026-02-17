using LetMeHatchThisEgg.Config;
using LetMeHatchThisEgg.Framework;
using LetMeHatchThisEgg.Helpers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings.ItemIds;

using static LetMeHatchThisEgg.ModEntry;

namespace LetMeHatchThisEgg.GameAssetManagement
{
    public class BirthTextEditor
    {
        private readonly IWildEggsApi _wildEggs;

        public BirthTextEditor(IWildEggsApi wildEggsApi)
        {
            _wildEggs = wildEggsApi;
        }

        public void EditBirthText(IDictionary<string, FarmAnimalData> data)
        {
            var exclusions = GetExclusions();
            ModEntry.Config.EditedAnimals[EditType.BirthText] = new();
            EditBirthTextDefault(data, exclusions);
            if (_wildEggs != null)
            {
                if (_wildEggs.BirthTextMode == "Mysterious")
                {
                    EditBirthTextMysterious(data, exclusions);
                }
                else
                {
                    EditBirthTextDefault(data, exclusions, true);
                }
            }
        }

        private List<string> GetExclusions()
        {
            return ModEntry
                .Config.EditedAnimals[EditType.BirthText]
                .Where(a => !a.Value)
                .Select(a => a.Key)
                .Union(VanillaAnimals.AnimalKeys)
                .ToList();
        }

        private void EditBirthTextDefault(
            IDictionary<string, FarmAnimalData> data,
            List<string> exclusions,
            bool isWildEggs = false
        )
        {
            foreach (var (key, animal) in data.Where(x => GetBirthTextCondition(x, isWildEggs)))
            {
                if (!exclusions.Contains(key))
                {
                    animal.BirthText = GetBirthText(animal);
                    ModEntry
                        .Config.EditedAnimals[EditType.BirthText]
                        .TryAdd(key, !exclusions.Contains(key));
                }
            }
        }

        private void EditBirthTextMysterious(
            IDictionary<string, FarmAnimalData> data,
            List<string> exclusions
        )
        {
            foreach (var (key, animal) in data.Where(x => GetBirthTextCondition(x, true)))
            {
                if (!exclusions.Contains(key))
                {
                    animal.BirthText = null;
                    ModEntry
                        .Config.EditedAnimals[EditType.BirthText]
                        .TryAdd(key, !exclusions.Contains(key));
                }
            }
        }

        private string GetBirthText(FarmAnimalData animal)
        {
            return ModEntry.Config.AlternateBirthText
                ? $"{ModEntry.HelperInstance?.Translation.Get("message.fullbirthtext")}: {animal.DisplayName}!"
                : $"{ModEntry.HelperInstance?.Translation.Get("message.new")} {animal.DisplayName} {ModEntry.HelperInstance?.Translation.Get("message.hatched")}!";
        }

        private bool GetBirthTextCondition(
            KeyValuePair<string, FarmAnimalData> animal,
            bool isWildEggs = false
        )
        {
            if (isWildEggs)
            {
                return _wildEggs.ValidAnimals.Contains(animal.Key);
            }
            else
            {
                return animal.Value.BirthText == null
                    && CheckIfProduceEgg(
                        animal
                            .Value.ProduceItemIds.Concat(animal.Value.DeluxeProduceItemIds)
                            .ToList()
                    );
            }
        }

        public bool CheckIfProduceEgg(List<FarmAnimalProduce> animalProduces)
        {
            if (animalProduces != null)
            {
                if (
                    animalProduces.Exists(x =>
                        x.ItemId.ToLower().Contains("egg")
                        || (
                            ModEntry.Config.CloneVanillaEggs
                            && AnimalProducts.VanillaEggs.Contains(x.ItemId)
                        )
                    )
                )
                {
                    return true;
                }
                else
                {
                    var produceEggCategory = animalProduces.Select(x => x.ItemId);
                    //ModEntry.HelperInstance?.GameContent.InvalidateCache("Data/Objects");

                    foreach ((var key, var objectID) in Game1.objectData)
                    {
                        if (
                            objectID.Category == StardewValley.Object.EggCategory
                            && produceEggCategory.Contains(key, StringComparer.OrdinalIgnoreCase)
                        )
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}