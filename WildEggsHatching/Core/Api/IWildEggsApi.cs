using System.Collections.Generic;
using System.Collections.ObjectModel;
using WildEggsHatching.Core.Enums;

namespace WildEggsHatching.Api;

/// <summary>
/// Public API: query-only, stable surface.
/// </summary>
public interface IWildEggsApi
{
    int ApiVersion { get; }

    /// <summary>Farm animal IDs considered valid for WildEggs-related birth text behavior.</summary>
    IReadOnlyCollection<string> ValidAnimals { get; }

    /// <summary>Object IDs for eggs treated as "wild eggs" by this mod.</summary>
    IReadOnlyCollection<string> WildEggsIDs { get; }

    /// <summary>Birth text mode string used by downstream mods (e.g. "Mysterious").</summary>
    string BirthTextMode { get; }

    ProgressionStep GetProgressionStep();

    bool IsProgressionModeEnabled();

    bool IsDesertTombModuleEnabled();

    IReadOnlyDictionary<string, int> GetQuestCompletionDays();
}
