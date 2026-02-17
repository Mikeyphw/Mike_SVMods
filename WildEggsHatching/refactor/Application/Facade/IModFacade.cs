using System.Collections.Generic;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Config;

namespace WildEggsHatching.Application.Facade;

/// <summary>
/// Single service façade used by console commands and the public API to keep behavior consistent.
/// </summary>
public interface IModFacade
{
    ProgressionStep GetProgressionStep();

    ModConfig GetConfigSnapshot();

    IReadOnlyDictionary<string, int> GetQuestCompletionDays();

    FacadeResult ReloadAll();

    FacadeResult TrySetProgressionStep(ProgressionStep step, bool applySideEffects, string reason);

    FacadeResult TryPlaceItem(string locationName, string qualifiedItemId, int count, int maxAttempts);

    FacadeResult TryRunSpawnPlanNow();

    FacadeResult DumpAnimalNotes();

    FacadeResult ValidateAnimalNotes();
}
