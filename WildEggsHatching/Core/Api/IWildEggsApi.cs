using System.Collections.Generic;
using WildEggsHatching.Core.Enums;

namespace WildEggsHatching.Api;

/// <summary>
/// Public API: query-only, stable surface.
/// </summary>
public interface IWildEggsApi
{
    int ApiVersion { get; }

    /// <summary>Farm animal IDs considered valid for WildEggs-related birth text behavior.</summary>
    HashSet<string> ValidAnimals { get; }

    void SetValidAnimals(HashSet<string> animals);

    /// <summary>Object IDs for eggs treated as "wild eggs" by this mod.</summary>
    HashSet<string> WildEggsIDs { get; }

    void SetWildEggsIDs(HashSet<string> ids);

    /// <summary>Birth text mode string used by downstream mods (e.g. "Mysterious").</summary>
    string BirthTextMode { get; }

    void SetBirthTextMode(string mode);

    string? ChosenAnimal_Coop { get; }
    string? ChosenAnimal_Barn { get; }
    string? ChosenAnimal_MagicCoop { get; }
    string? ChosenAnimal_MagicBarn { get; }

    void SetChosenAnimal(string chosenAnimal, string home, bool isMagic);

    ProgressionStep GetProgressionStep();

    bool IsProgressionModeEnabled();

    bool IsDesertTombModuleEnabled();

    IReadOnlyDictionary<string, int> GetQuestCompletionDays();
}
