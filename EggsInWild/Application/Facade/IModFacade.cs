using System.Collections.Generic;
using EggsInWild.Application.AnimalNotes;
using EggsInWild.Core.Enums;
using EggsInWild.Infrastructure.Config;

namespace EggsInWild.Application.Facade;

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

    FacadeResult PreviewStepEffects(ProgressionStep step);

    FacadeResult TryPlaceItem(string locationName, string qualifiedItemId, int count, int maxAttempts);

    FacadeResult TryRunSpawnPlanNow();

    FacadeResult DumpAnimalNotes();

    FacadeResult DumpAnimalNotes(AnimalNotesDumpOptions options);

    FacadeResult RepairAnimalNotes(bool requeueMail = true, bool reconcileNotes = true, bool force = false);

    FacadeResult ValidateAnimalNotes();

    FacadeResult Verify(string[] args);

    FacadeResult WipeSchemaData(bool confirmed);

#if WEGG_DEV
    /// <summary>Dev-only: force-install DesertTomb Harmony patches immediately (bypasses config gating).</summary>
    FacadeResult ForceInstallDesertTombHarmonyPatches();

    /// <summary>Dev-only: dump effective wild animals + resolved farm animal keys as JSON.</summary>
    FacadeResult DumpEffectiveWildAnimals();

    /// <summary>Dev-only: return JSON describing one wild animal's effective values and config overrides.</summary>
    FacadeResult WildAnimalStatus(string wildAnimalKey);

    /// <summary>Dev-only: set the Enabled override for one wild animal (persists config).</summary>
    FacadeResult SetWildAnimalEnabled(string wildAnimalKey, bool enabled);

    /// <summary>Dev-only: export effective ExtraModAnimalKeys as bulk lines (modId | csvKeys) for one wild animal.</summary>
    FacadeResult ExportWildAnimalEffectiveBulk(string wildAnimalKey);

    /// <summary>Dev-only: validate a bulk-text payload (modId | csvKeys) for one wild animal; supports @filePath and \\n escapes.</summary>
    FacadeResult ValidateWildAnimalBulk(string wildAnimalKey, string rawOrPath);
#endif
}

