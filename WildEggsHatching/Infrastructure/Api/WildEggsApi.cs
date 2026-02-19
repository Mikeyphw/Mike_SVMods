using System;
using System.Collections.Generic;
using System.Linq;
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
}
