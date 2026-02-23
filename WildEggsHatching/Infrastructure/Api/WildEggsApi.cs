using System;
using System.Collections.Generic;
using System.Linq;
using Mikew.BirdsOfAFeather.EnumsAndStrings;
using WildEggsHatching.Api;
using WildEggsHatching.Application.Facade;
using WildEggsHatching.Core.Enums;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.GameData.FarmAnimals;

namespace WildEggsHatching.Infrastructure.Api;

public sealed class WildEggsApi : IWildEggsApi
{
    private readonly IModFacade _facade;

    private HashSet<string>? _cachedWildEggIds;
    private HashSet<string>? _cachedValidAnimals;

    private string _birthTextMode = "Default";
    private string? _chosenCoop;
    private string? _chosenBarn;
    private string? _chosenMagicCoop;
    private string? _chosenMagicBarn;

    public WildEggsApi(IModFacade facade) => _facade = facade;

    public int ApiVersion => 1;

    public HashSet<string> WildEggsIDs => _cachedWildEggIds ??= ComputeWildEggIds();

    public void SetWildEggsIDs(HashSet<string> ids) => _cachedWildEggIds = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);

    public HashSet<string> ValidAnimals => _cachedValidAnimals ??= ComputeValidAnimals();

    public void SetValidAnimals(HashSet<string> animals) => _cachedValidAnimals = new HashSet<string>(animals, StringComparer.OrdinalIgnoreCase);

    public string BirthTextMode => _birthTextMode;

    public void SetBirthTextMode(string mode) => _birthTextMode = string.IsNullOrWhiteSpace(mode) ? "Default" : mode;

    public string? ChosenAnimal_Coop => _chosenCoop;
    public string? ChosenAnimal_Barn => _chosenBarn;
    public string? ChosenAnimal_MagicCoop => _chosenMagicCoop;
    public string? ChosenAnimal_MagicBarn => _chosenMagicBarn;

    public void SetChosenAnimal(string chosenAnimal, string home, bool isMagic)
    {
        if (string.IsNullOrWhiteSpace(chosenAnimal) || string.IsNullOrWhiteSpace(home))
            return;

        var isCoop = home.Equals("Coop", StringComparison.OrdinalIgnoreCase);
        if (isMagic)
        {
            if (isCoop) _chosenMagicCoop = chosenAnimal;
            else _chosenMagicBarn = chosenAnimal;
        }
        else
        {
            if (isCoop) _chosenCoop = chosenAnimal;
            else _chosenBarn = chosenAnimal;
        }
    }

    public ProgressionStep GetProgressionStep() => _facade.GetProgressionStep();

    public bool IsProgressionModeEnabled() => _facade.GetConfigSnapshot().ProgressionMode;

    public bool IsDesertTombModuleEnabled() => _facade.GetConfigSnapshot().DesertTombModuleEnabled;

    public IReadOnlyDictionary<string, int> GetQuestCompletionDays() => _facade.GetQuestCompletionDays();

    private HashSet<string> ComputeWildEggIds()
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var data = Game1.content.Load<Dictionary<string, ObjectData>>(Strings.AssetNames.Objects);
            if (data is null || data.Count == 0)
                return ids;

            foreach (var (id, obj) in data)
            {
                if (string.IsNullOrWhiteSpace(id) || obj is null)
                    continue;

                if (IsWildEggObject(id, obj))
                    ids.Add(id);
            }
        }
        catch
        {
            // best-effort: API callers can also inject IDs via SetWildEggsIDs.
        }

        return ids;
    }

    private HashSet<string> ComputeValidAnimals()
    {
        var animals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var wildEggIds = _cachedWildEggIds ??= ComputeWildEggIds();
        if (wildEggIds.Count == 0)
            return animals;

        try
        {
            var data = Game1.content.Load<Dictionary<string, FarmAnimalData>>(Strings.AssetNames.FarmAnimals);
            if (data is null || data.Count == 0)
                return animals;

            foreach (var (animalId, animal) in data)
            {
                if (string.IsNullOrWhiteSpace(animalId) || animal?.EggItemIds is null || animal.EggItemIds.Count == 0)
                    continue;

                if (animal.EggItemIds.Any(id => !string.IsNullOrWhiteSpace(id) && wildEggIds.Contains(id)))
                    animals.Add(animalId);
            }
        }
        catch
        {
            // best-effort: API callers can also inject valid animals via SetValidAnimals.
        }

        return animals;
    }

    private static bool IsWildEggObject(string id, ObjectData obj)
    {
        if (obj.ContextTags?.Any(tag => string.Equals(tag, "wildegg_item", StringComparison.OrdinalIgnoreCase)) == true)
            return true;

        if (obj.CustomFields is not null)
        {
            if (obj.CustomFields.ContainsKey("wildEggLocation")
                || obj.CustomFields.ContainsKey("WildEggs.Kind")
                || obj.CustomFields.ContainsKey("WildEggs.Region"))
                return true;
        }

        var prefix = WildEggsHatching.Constants.ModId + "_";
        if (id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var suffix in new[] { "LMWEgg", "LWEgg", "MWEgg", "WEgg" })
            {
                if (id.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

}
