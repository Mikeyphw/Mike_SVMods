using System;
using System.Collections.Generic;
using System.Linq;
using Mikew.BirdsOfAFeather.Framework;
using StardewModdingAPI;
using WildEggsHatching.Application.Animals;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.ModData;

namespace WildEggsHatching.Infrastructure.Api;

/// <summary>
/// Shared public API for downstream mods (e.g. LetMeHatchThisEgg).
/// This contract lives in BOAF: <see cref="IWildEggsApi"/>.
/// </summary>
public sealed class WildEggsApi : IWildEggsApi
{
    private readonly IModDataLoader _data;
    private readonly IModRegistry _registry;
    private readonly ChosenEggAssignments _chosen;

    private HashSet<string>? _cachedWildEggIds;
    private HashSet<string>? _cachedValidAnimals;

    private string _birthTextMode = "Default";

    private string? _overrideCoop;
    private string? _overrideBarn;
    private string? _overrideMagicCoop;
    private string? _overrideMagicBarn;

    public WildEggsApi(IModDataLoader data, IModRegistry registry, ChosenEggAssignments chosen)
    {
        _data = data;
        _registry = registry;
        _chosen = chosen;
    }

    public int ApiVersion => 2;

    public HashSet<string> WildEggsIDs => _cachedWildEggIds ??= ComputeWildEggIds();

    public void SetWildEggsIDs(HashSet<string> ids)
        => _cachedWildEggIds = new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);

    public HashSet<string> ValidAnimals => _cachedValidAnimals ??= ComputeValidAnimals();

    public void SetValidAnimals(HashSet<string> animals)
        => _cachedValidAnimals = new HashSet<string>(animals, StringComparer.OrdinalIgnoreCase);

    public string BirthTextMode => _birthTextMode;

    public void SetBirthTextMode(string mode)
        => _birthTextMode = string.IsNullOrWhiteSpace(mode) ? "Default" : mode;

    public string? ChosenAnimal_Coop => _overrideCoop ?? TryResolveChosen(home: "Coop", isMagic: false);
    public string? ChosenAnimal_Barn => _overrideBarn ?? TryResolveChosen(home: "Barn", isMagic: false);
    public string? ChosenAnimal_MagicCoop => _overrideMagicCoop ?? TryResolveChosen(home: "Coop", isMagic: true);
    public string? ChosenAnimal_MagicBarn => _overrideMagicBarn ?? TryResolveChosen(home: "Barn", isMagic: true);

    public void SetChosenAnimal(string chosenAnimal, string home, bool isMagic)
    {
        if (string.IsNullOrWhiteSpace(chosenAnimal) || string.IsNullOrWhiteSpace(home))
            return;

        var isCoop = home.Equals("Coop", StringComparison.OrdinalIgnoreCase);
        if (isMagic)
        {
            if (isCoop) _overrideMagicCoop = chosenAnimal;
            else _overrideMagicBarn = chosenAnimal;
        }
        else
        {
            if (isCoop) _overrideCoop = chosenAnimal;
            else _overrideBarn = chosenAnimal;
        }
    }

    private HashSet<string> ComputeWildEggIds()
    {
        var snapshot = _data.Load();
        return new HashSet<string>(snapshot.SpawnAreas.Keys, StringComparer.OrdinalIgnoreCase);
    }

    private HashSet<string> ComputeValidAnimals()
    {
        var snapshot = _data.Load();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var wildAnimal in snapshot.WildAnimals.Values)
        {
            foreach (var kvp in wildAnimal.ExtraModAnimalKeys)
            {
                var modId = kvp.Key;
                var block = kvp.Value;

                if (!IsEligibleModBlock(modId))
                    continue;

                foreach (var id in block.FarmAnimalKeysSameWeight)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                        ids.Add(id);
                }

                foreach (var w in block.FarmAnimalKeysWeighted)
                {
                    if (!string.IsNullOrWhiteSpace(w.Id))
                        ids.Add(w.Id);
                }
            }
        }

        return ids;
    }

    private bool IsEligibleModBlock(string modUniqueId)
        => string.Equals(modUniqueId, "Vanilla", StringComparison.OrdinalIgnoreCase)
           || _registry.IsLoaded(modUniqueId);

    private string? TryResolveChosen(string home, bool isMagic)
    {
        // Best-effort: infer from the chosen egg mapping by egg kind.
        foreach (var (eggId, animalId) in _chosen.Current)
        {
            var kind = EggKindExtensions.FromEggItemId(eggId);

            if (kind.IsMagic() != isMagic)
                continue;

            var needsLarge = home.Equals("Barn", StringComparison.OrdinalIgnoreCase);
            if (kind.IsLarge() != needsLarge)
                continue;

            return animalId;
        }

        return null;
    }
}
