using System.Collections.Generic;
using WildEggsHatching.Core.Enums;

namespace WildEggsHatching.Api;

/// <summary>
/// Public API: query-only, stable surface.
/// </summary>
public interface IWildEggsApi
{
    int ApiVersion { get; }

    ProgressionStep GetProgressionStep();

    bool IsProgressionModeEnabled();

    bool IsDesertTombModuleEnabled();

    IReadOnlyDictionary<string, int> GetQuestCompletionDays();
}
