using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LetMeHatchThisEgg.Helpers;
using Mikew.BirdsOfAFeather.Helpers;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;
using Object = StardewValley.Object;

namespace LetMeHatchThisEgg.GameAssetManagement;

/// <summary>
/// Populates <see cref="FarmAnimalData.EggItemIds"/> based on produce entries.
/// </summary>
public sealed class EggListEditor
{
    private readonly CloningHelper _cloningHelper;

    public EggListEditor(CloningHelper cloningHelper)
    {
        _cloningHelper = cloningHelper;
    }

    public void SetUpEggList(IDictionary<string, FarmAnimalData> data)
    {
        var exclusions = GetExclusions();
        ModEntry.Config.EditedAnimals[EditType.EggList] = new();

        foreach (var (key, animal) in data)
        {
            var eggsItems = SearchProduces(animal.ProduceItemIds.Concat(animal.DeluxeProduceItemIds).ToList());

            if (animal.EggItemIds != null)
            {
                RemoveClonedEggs(animal, eggsItems);
                UpdateEggItemIds(animal, eggsItems, key, exclusions);
            }
            else if (eggsItems.Any())
            {
                animal.EggItemIds = eggsItems;
                TryAddEditedAnimal(key, exclusions);
            }
        }
    }

    public List<string> SearchProduces(List<FarmAnimalProduce> animalProduces)
    {
        var eggsItems = new List<string>();
        if (animalProduces == null)
        {
            return eggsItems;
        }

        var pattern = @".*egg.*";
        var foundEggs = animalProduces
            .Select(p => p.ItemId)
            .Where(pid => !string.IsNullOrEmpty(pid) && CheckIfIsAValidEgg(pid, pattern, Game1.objectData));

        eggsItems.AddRange(foundEggs);
        return eggsItems;
    }

    private static List<string> GetExclusions()
    {
        return ModEntry.Config
            .EditedAnimals[EditType.EggList]
            .Where(a => !a.Value)
            .Select(a => a.Key)
            .ToList();
    }

    private void RemoveClonedEggs(FarmAnimalData animal, List<string> eggsItems)
    {
        foreach (var egg in eggsItems.Where(CloningHelper.IsClonedEgg))
        {
            var originalEgg = _cloningHelper.GetOriginalItemAndAnimalID(egg).OriginalItemID;
            animal.EggItemIds.RemoveAll(e => e == originalEgg);
        }
    }

    private static void UpdateEggItemIds(
        FarmAnimalData animal,
        List<string> eggsItems,
        string key,
        List<string> exclusions
    )
    {
        var oldList = new List<string>(animal.EggItemIds);
        animal.EggItemIds.AddRange(eggsItems.Except(animal.EggItemIds));

        if (!oldList.OrderBy(x => x).SequenceEqual(animal.EggItemIds.OrderBy(x => x)) && !exclusions.Contains(key))
        {
            TryAddEditedAnimal(key, exclusions);
        }
    }

    private static void TryAddEditedAnimal(string key, List<string> exclusions)
    {
        if (!exclusions.Contains(key))
        {
            ModEntry.Config.EditedAnimals[EditType.EggList].TryAdd(key, true);
        }
    }

    private static bool CheckIfIsAValidEgg(
        string? itemId,
        string pattern,
        IDictionary<string, ObjectData> gameObjectData
    )
    {
        if (string.IsNullOrEmpty(itemId))
        {
            LoggerHelper.VerboseLog("Item ID is null or empty, not finding valid egg");
            return false;
        }

        if (!gameObjectData.ContainsKey(itemId))
        {
            return CloningHelper.IsClonedEgg(itemId);
        }

        return Regex.IsMatch(itemId, pattern, RegexOptions.IgnoreCase)
            || (
                gameObjectData[itemId].Category == Object.EggCategory
                && Regex.IsMatch(
                    int.TryParse(itemId, out _) ? gameObjectData[itemId].DisplayName : itemId,
                    pattern,
                    RegexOptions.IgnoreCase
                )
            );
    }
}
