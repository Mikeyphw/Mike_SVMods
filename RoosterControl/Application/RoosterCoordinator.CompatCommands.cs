using System;
using System.Linq;
using StardewModdingAPI;
using RoosterControl.Application.Compat;
using Mikew.NestCore.Helpers.Logging;

using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;
using System.Collections.Generic;
using System.Text;
using StardewValley.GameData.FarmAnimals;
using RoosterControl.Enums;

namespace RoosterControl.Application;

internal sealed partial class RoosterCoordinator
{
    private void CompatCommand(string command, string[] args)
    {
        var verb = args.FirstOrDefault()?.Trim() ?? "help";
        var verbLower = verb.ToLowerInvariant();

        if (verbLower is "help" or "-h" or "--help")
        {
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                "ra compat usage:\n" +
                "  ra compat list\n" +
                "    - prints current compat catalogs and per-save selections.\n" +
                "  ra compat status\n" +
                "    - prints catalog health warnings + current selections (single block).\n" +
                "  ra compat cycle-egg <EggKind> [prev]\n" +
                "    - cycles selected egg source for the given EggKind.\n" +
                "  ra compat cycle-rooster [prev]\n" +
                "    - cycles rooster provider in single-provider mode.\n" +
                "  ra compat enable-all-roosters\n" +
                "    - enables all rooster providers (union mode).\n" +
                "  ra compat validate [EggKind]\n" +
                "    - reports invalid FarmAnimalRef targets/indexes and shows resolved values.\n" +
                "  ra compat reset-help\n" +
                "    - prints reset commands + examples.\n" +
                "\n" +
                "Notes:\n" +
                "  - Mutating commands are host-only.\n" +
                "  - Changes persist immediately to save data.",
                LogLevel.Info);
            return;
        }

        if (verbLower is "reset-help" or "resethelp")
        {
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, BuildCompatResetHelp(), LogLevel.Info);
            return;
        }

        if (!Context.IsWorldReady)
        {
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                "ra compat is only available after a save is loaded.",
                LogLevel.Info);
            return;
        }

        EnsureCompatInitialized();

        if (verbLower == "status")
        {
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, BuildCompatStatusSummary(), LogLevel.Info);
            return;
        }

        if (verbLower == "list")
        {
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, _compatSelection.BuildStatusReport(_saveState.Compat), LogLevel.Info);
            return;
        }

if (verbLower == "validate")
{
    EggKind? filter = null;
    if (args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1]))
    {
        if (!Enum.TryParse<EggKind>(args[1], ignoreCase: true, out var parsed))
        {
            var kinds = string.Join(", ", CompatSelectionService.ManagedEggKinds);
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                $"Unknown EggKind '{args[1]}'. Valid values: {kinds}",
                LogLevel.Info);
            return;
        }

        if (!CompatSelectionService.ManagedEggKinds.Contains(parsed))
        {
            var kinds = string.Join(", ", CompatSelectionService.ManagedEggKinds);
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                $"EggKind '{parsed}' is not managed by compat. Valid values: {kinds}",
                LogLevel.Info);
            return;
        }

        filter = parsed;
    }

    Dictionary<string, FarmAnimalData> animalData;
    try
    {
        animalData = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
    }
    catch (Exception ex)
    {
        LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
            $"Failed to load {FarmAnimalsAssetName}: {ex.Message}",
            LogLevel.Warn);
        return;
    }

    var kindsToCheck = filter.HasValue
        ? new[] { filter.Value }
        : CompatSelectionService.ManagedEggKinds.ToArray();

    var sb = new StringBuilder();
    sb.AppendLine("ra compat validate:");
    sb.AppendLine($"  FarmAnimals loaded: {animalData.Count}");
    sb.AppendLine($"  EggKinds: {string.Join(", ", kindsToCheck)}");

    var totalRefs = 0;
    var totalInvalid = 0;
    var selectedErrors = 0;
    var warnings = 0;

    foreach (var kind in kindsToCheck)
    {
        sb.AppendLine();
        sb.AppendLine($"EggKind: {kind}");

        if (_compatSelection.TryResolveSelectedEggItemId(_saveState.Compat, kind, animalData, out var selectedItemId, out var selectedError))
        {
            _saveState.Compat.SelectedEggSources.TryGetValue(kind, out var selectedId);
            sb.AppendLine($"  Selected: {selectedId} -> {selectedItemId}");
        }
        else
        {
            selectedErrors++;
            sb.AppendLine($"  Selected: ERROR -> {selectedError}");
            if (!string.IsNullOrWhiteSpace(selectedError) && selectedError.Contains("not found in Data/FarmAnimals", StringComparison.OrdinalIgnoreCase))
                warnings++;

            sb.AppendLine($"    Fix (GMCM): Compat → Egg sources → {kind}");
            sb.AppendLine($"    Fix (console): ra compat cycle-egg {kind}");
            sb.AppendLine($"    Fix (reset): ra compat reset-eggs {kind}");
            sb.AppendLine($"    Fix (save data): Compat.SelectedEggSources[{kind}] = \"<SourceId>\"");
        }

        var sources = _compatSelection.EggCatalog.Sources
            .Where(s => s.EggKind == kind)
            .ToList();

        var directCount = sources.Count(s => !string.IsNullOrWhiteSpace(s.ItemId));
        var refSources = sources.Where(s => s.FarmAnimalRef is not null).ToList();

        var invalidThisKind = 0;
        foreach (var src in refSources)
        {
            totalRefs++;
            if (_compatSelection.TryResolveEggSourceItemId(src, animalData, out var resolved, out var err))
                continue;

            invalidThisKind++;
            totalInvalid++;
            var fa = src.FarmAnimalRef!;
            sb.AppendLine($"  INVALID: {src.SourceId} ({src.DisplayName}) -> {fa.FarmAnimalKey}.{fa.List}[{fa.Index}] :: {err}");
            if (!string.IsNullOrWhiteSpace(err) && err.Contains("not found in Data/FarmAnimals", StringComparison.OrdinalIgnoreCase))
            {
                warnings++;
                sb.AppendLine("    WARN: FarmAnimal key is missing. Likely a missing/disabled optional mod or content pack.");
            }

            sb.AppendLine($"    Fix: pick a different egg source for {kind} (GMCM Compat page), or install/enable the content that provides {fa.FarmAnimalKey}.");
        }

        sb.AppendLine($"  Catalog: total={sources.Count}, directItemId={directCount}, farmAnimalRef={refSources.Count}, invalidRefs={invalidThisKind}");
    }
    var enabledProviders = _saveState.Compat.EnabledRoosterSources?
        .Where(id => !string.IsNullOrWhiteSpace(id))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count() ?? 0;

    var roosterKeys = _compatSelection.GetEnabledRoosterFarmAnimalKeys(_saveState.Compat);
    sb.AppendLine();
    sb.AppendLine($"Rooster providers enabled: {enabledProviders}");
    sb.AppendLine($"Coop rooster FarmAnimal keys: {roosterKeys.Count}");

    var knownProviderIds = new HashSet<string>(
        _compatSelection.RoosterCatalog.Sources.Select(s => s.SourceId),
        StringComparer.OrdinalIgnoreCase
    );
    var enabledProviderIds = _saveState.Compat.EnabledRoosterSources?
        .Where(id => !string.IsNullOrWhiteSpace(id))
        .Select(id => id.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList() ?? new List<string>();
    var unknownProviders = enabledProviderIds
        .Where(id => !knownProviderIds.Contains(id))
        .ToList();

    if (unknownProviders.Count > 0)
    {
        warnings += unknownProviders.Count;
        sb.AppendLine();
        sb.AppendLine("Rooster provider issues:");
        foreach (var id in unknownProviders)
        {
            sb.AppendLine($"  WARN: EnabledRoosterSources contains unknown provider id '{id}'.");
        }
        sb.AppendLine("    Fix (reset): ra compat reset-roosters");
        sb.AppendLine("    Fix (save data): Compat.EnabledRoosterSources = [ ... ]");
    }

    var missingRoosterKeys = roosterKeys
        .Where(k => !animalData.ContainsKey(k))
        .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
        .ToList();
    if (missingRoosterKeys.Count > 0)
    {
        warnings += missingRoosterKeys.Count;
        sb.AppendLine();
        sb.AppendLine("Rooster FarmAnimal key issues:");
        foreach (var k in missingRoosterKeys)
            sb.AppendLine($"  WARN: Rooster FarmAnimal key '{k}' not found in Data/FarmAnimals (optional mod/pack missing?).");
        sb.AppendLine("    Fix: install/enable the mod/content pack that adds those farm animals, or change rooster providers in GMCM.");
    }

    sb.AppendLine();
    sb.AppendLine($"Warnings: {warnings}");
    sb.AppendLine();
    sb.AppendLine($"Summary: refsChecked={totalRefs}, invalidRefs={totalInvalid}, selectedErrors={selectedErrors}, warnings={warnings}");

    var level = totalInvalid > 0 || selectedErrors > 0 || warnings > 0 ? LogLevel.Warn : LogLevel.Info;
    LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, sb.ToString(), level);
    EnsureCompatValidationState(animalData, reason: "ra_compat validate", log: false, category: ModRoosterControlCategory.Commands);
    return;
}


        if (verbLower == "reset-all")
        {
            if (!Context.IsMainPlayer)
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    "Only the host can change compat selections.",
                    LogLevel.Info);
                return;
            }

            if (TryResetCompatSelections(resetEggSources: true, resetRoosterProviders: true, reason: "ra_compat reset-all", out var summary))
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    summary is null
                        ? "Compat selections reset to defaults."
                        : $"Compat selections reset to defaults:\n{summary}",
                    LogLevel.Info);
            }

            return;
        }

        if (verbLower == "reset-eggs")
        {
            if (!Context.IsMainPlayer)
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    "Only the host can change compat selections.",
                    LogLevel.Info);
                return;
            }

            if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]))
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    "Usage: ra compat reset-eggs <EggKind|all>",
                    LogLevel.Info);
                return;
            }

            var target = args[1].Trim();

            if (target.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                if (TryResetCompatSelections(resetEggSources: true, resetRoosterProviders: false, reason: "ra_compat reset-eggs all", out var summary))
                {
                    LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                        summary is null
                            ? "Egg sources reset to defaults."
                            : $"Egg sources reset to defaults:\n{summary}",
                        LogLevel.Info);
                }

                return;
            }

            if (!Enum.TryParse<EggKind>(target, ignoreCase: true, out var kind))
            {
                var kinds = string.Join(", ", CompatSelectionService.ManagedEggKinds);
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    $"Unknown EggKind '{target}'. Valid values: {kinds} (or 'all')",
                    LogLevel.Info);
                return;
            }

            if (!CompatSelectionService.ManagedEggKinds.Contains(kind))
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    $"EggKind '{kind}' is not managed by compat.",
                    LogLevel.Info);
                return;
            }

            if (TryResetCompatEggKind(kind, reason: $"ra_compat reset-eggs {kind}", out var oneSummary))
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    oneSummary is null
                        ? $"Egg source reset to default for {kind}."
                        : $"Egg source reset to default for {kind}:\n{oneSummary}",
                    LogLevel.Info);
            }

            return;
        }

        if (verbLower == "reset-roosters")
        {
            if (!Context.IsMainPlayer)
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    "Only the host can change compat selections.",
                    LogLevel.Info);
                return;
            }

            if (TryResetCompatSelections(resetEggSources: false, resetRoosterProviders: true, reason: "ra_compat reset-roosters", out var summary))
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    summary is null
                        ? "Rooster providers reset to defaults."
                        : $"Rooster providers reset to defaults:\n{summary}",
                    LogLevel.Info);
            }

            return;
        }

        if (verbLower == "cycle-egg")
        {
            if (!Context.IsMainPlayer)
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    "Only the host can change compat selections.",
                    LogLevel.Info);
                return;
            }

            if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]))
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    "Usage: ra compat cycle-egg <EggKind> [prev]",
                    LogLevel.Info);
                return;
            }

            if (!Enum.TryParse<EggKind>(args[1], ignoreCase: true, out var kind))
            {
                var kinds = string.Join(", ", CompatSelectionService.ManagedEggKinds);
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    $"Unknown EggKind '{args[1]}'. Valid values: {kinds}",
                    LogLevel.Info);
                return;
            }

            if (!CompatSelectionService.ManagedEggKinds.Contains(kind))
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    $"EggKind '{kind}' is not managed by compat.",
                    LogLevel.Info);
                return;
            }

            var prev = args.Skip(2).Any(a => a.Equals("prev", StringComparison.OrdinalIgnoreCase));
            if (!_compatSelection.TryCycleEggSource(_saveState.Compat, kind, prev, out var selected, out var changed, out var error))
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    error ?? "Failed to cycle egg source.",
                    LogLevel.Info);
                return;
            }

            if (changed)
                PersistCompatSelectionChange();

            var msg = selected is null
                ? $"Egg source for '{kind}' {(changed ? "updated" : "unchanged")}."
                : changed
                    ? $"Egg source for '{kind}' -> '{selected.SourceId}' ({selected.DisplayName})"
                    : $"Egg source for '{kind}' unchanged -> '{selected.SourceId}' ({selected.DisplayName})";
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, msg, LogLevel.Info);
            return;
        }

        if (verbLower == "cycle-rooster")
        {
            if (!Context.IsMainPlayer)
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    "Only the host can change compat selections.",
                    LogLevel.Info);
                return;
            }

            var prev = args.Skip(1).Any(a => a.Equals("prev", StringComparison.OrdinalIgnoreCase));
            if (!_compatSelection.TryCycleRoosterProviderSingle(_saveState.Compat, prev, out var selected, out var changed, out var error))
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    error ?? "Failed to cycle rooster provider.",
                    LogLevel.Info);
                return;
            }

            if (changed)
                PersistCompatSelectionChange();

            var msg = selected is null
                ? $"Rooster provider (single) {(changed ? "updated" : "unchanged")}."
                : changed
                    ? $"Rooster provider (single) -> '{selected.SourceId}' ({selected.DisplayName})"
                    : $"Rooster provider (single) unchanged -> '{selected.SourceId}' ({selected.DisplayName})";
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, msg, LogLevel.Info);
            return;
        }

        if (verbLower == "enable-all-roosters")
        {
            if (!Context.IsMainPlayer)
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    "Only the host can change compat selections.",
                    LogLevel.Info);
                return;
            }

            if (!_compatSelection.EnableAllRoosterProviders(_saveState.Compat, out var enabledCount, out var changed, out var error))
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    error ?? "Failed to enable all rooster providers.",
                    LogLevel.Info);
                return;
            }

            if (changed)
                PersistCompatSelectionChange();

            var msg = changed
                ? $"Rooster providers (union) enabled: {enabledCount}"
                : $"Rooster providers (union) unchanged (already all enabled): {enabledCount}";
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, msg, LogLevel.Info);
            return;
        }

        LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
            $"Unknown ra compat verb '{verb}'. Use: ra compat help",
            LogLevel.Info);
    }

    private void PersistCompatSelectionChange()
    {
        _needsRoosterSetup = true;

        if (Context.IsMainPlayer)
            _saveData.Write(SaveStateKey, _saveState);

        InvalidateDailyPlanCache("compat selection changed");

        RequestInvalidate(FarmAnimalsAssetName);
        if (RoosterHelper.FemaleEggs.Count > 0)
            RequestInvalidate(ObjectsAssetName);

        QueueRefreshFarmAnimals("compat selection changed");
    }

    private static string BuildCompatResetHelp()
    {
        return
            "ra compat reset commands:\n" +
            "  ra compat reset-all\n" +
            "    - resets all per-save compat selections (egg sources + rooster providers) to defaults.\n" +
            "  ra compat reset-eggs <EggKind|all>\n" +
            "    - resets egg sources for one EggKind (or all) to defaults.\n" +
            "  ra compat reset-roosters\n" +
            "    - resets rooster providers to defaults (enable all if available).\n" +
            "\n" +
            "Examples:\n" +
            "  ra compat reset-eggs BrownEgg\n" +
            "  ra compat reset-eggs all\n" +
            "\n" +
            "Notes:\n" +
            "  - Mutating commands are host-only.\n" +
            "  - Changes persist immediately to save data.\n" +
            "  - GMCM reset toggles apply only after clicking Save in GMCM.";
    }



    private bool TryLoadFarmAnimals(out Dictionary<string, FarmAnimalData> farmAnimals, out string? error)
    {
        farmAnimals = new Dictionary<string, FarmAnimalData>();
        error = null;

        try
        {
            farmAnimals = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private sealed record CompatValidationCounts(
        int Warnings,
        int Errors,
        int InvalidRefErrors,
        int SelectedEggErrors,
        int UnknownProviderWarnings,
        int MissingRoosterKeyWarnings,
        IReadOnlyList<string> SelectedEggErrorSamples,
        IReadOnlyList<string> InvalidRefErrorSamples,
        IReadOnlyList<string> MissingRoosterKeySamples
    );

    private void EnsureCompatValidationState(
        IDictionary<string, FarmAnimalData> farmAnimals,
        string reason,
        bool log,
        ModRoosterControlCategory category
    )
    {
        if (!Context.IsWorldReady)
            return;

        EnsureCompatInitialized();

        var counts = ComputeCompatValidationCounts(farmAnimals);
        UpdateCompatValidationState(reason, counts);

        if (!log)
            return;

        var msg = BuildCompatValidationLogSummary(reason, counts);
        var level = counts.Errors > 0 || counts.Warnings > 0 ? LogLevel.Warn : LogLevel.Info;
        LogScopedHelper.Log(_log, category, msg, level);
    }

    private CompatValidationCounts ComputeCompatValidationCounts(IDictionary<string, FarmAnimalData> farmAnimals)
    {
        var warnings = 0;
        var errors = 0;

        var selectedEggErrors = 0;
        var selectedEggErrorSamples = new List<string>();
        foreach (var kind in CompatSelectionService.ManagedEggKinds)
        {
            if (_compatSelection.TryResolveSelectedEggItemId(_saveState.Compat, kind, farmAnimals, out _, out var err))
                continue;

            selectedEggErrors++;
            if (selectedEggErrorSamples.Count < 6)
            {
                var sample = string.IsNullOrWhiteSpace(err)
                    ? kind.ToString()
                    : $"{kind} ({err})";
                selectedEggErrorSamples.Add(sample);
            }

            if (IsMissingFarmAnimalKey(err))
                warnings++;
            else
                errors++;
        }

        var invalidRefErrors = 0;
        var invalidRefErrorSamples = new List<string>();
        foreach (var kind in CompatSelectionService.ManagedEggKinds)
        {
            foreach (var src in _compatSelection.EggCatalog.Sources.Where(s => s.EggKind == kind && s.FarmAnimalRef is not null))
            {
                if (_compatSelection.TryResolveEggSourceItemId(src, farmAnimals, out _, out var err))
                    continue;

                invalidRefErrors++;
                if (invalidRefErrorSamples.Count < 6)
                {
                    var refText = src.FarmAnimalRef is null
                        ? "INVALID(ref)"
                        : $"{src.FarmAnimalRef.FarmAnimalKey}/{src.FarmAnimalRef.List}[{src.FarmAnimalRef.Index}]";
                    var sample = string.IsNullOrWhiteSpace(err)
                        ? $"{src.SourceId} -> {refText}"
                        : $"{src.SourceId} -> {refText} ({err})";
                    invalidRefErrorSamples.Add(sample);
                }

                if (IsMissingFarmAnimalKey(err))
                    warnings++;
                else
                    errors++;
            }
        }

        var knownProviderIds = new HashSet<string>(
            _compatSelection.RoosterCatalog.Sources.Select(s => s.SourceId),
            StringComparer.OrdinalIgnoreCase
        );
        var enabledProviderIds = _saveState.Compat.EnabledRoosterSources?
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        var unknownProviderWarnings = enabledProviderIds.Count(id => !knownProviderIds.Contains(id));
        warnings += unknownProviderWarnings;

        var roosterKeys = _compatSelection.GetEnabledRoosterFarmAnimalKeys(_saveState.Compat);
        var missingRoosterKeyWarnings = roosterKeys.Count(k => !farmAnimals.ContainsKey(k));
        var missingRoosterKeySamples = roosterKeys
            .Where(k => !farmAnimals.ContainsKey(k))
            .Take(6)
            .ToList();
        warnings += missingRoosterKeyWarnings;

        return new CompatValidationCounts(
            Warnings: warnings,
            Errors: errors,
            InvalidRefErrors: invalidRefErrors,
            SelectedEggErrors: selectedEggErrors,
            UnknownProviderWarnings: unknownProviderWarnings,
            MissingRoosterKeyWarnings: missingRoosterKeyWarnings,
            SelectedEggErrorSamples: selectedEggErrorSamples,
            InvalidRefErrorSamples: invalidRefErrorSamples,
            MissingRoosterKeySamples: missingRoosterKeySamples
        );
    }

    private static bool IsMissingFarmAnimalKey(string? error)
    {
        return !string.IsNullOrWhiteSpace(error)
            && error.Contains("not found in Data/FarmAnimals", StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateCompatValidationState(string reason, CompatValidationCounts counts)
    {
        _compatValidationLastRunAt = DateTimeOffset.Now;
        _compatValidationLastReason = reason;
        _compatValidationWarnings = counts.Warnings;
        _compatValidationErrors = counts.Errors;

        var at = _compatValidationLastRunAt.Value.ToString("yyyy-MM-dd HH:mm:ss zzz");
        if (counts.Warnings == 0 && counts.Errors == 0)
        {
            _compatValidationSummary = $"✅ Validation OK ({at})\nNo issues found.\nRun: ra verify compat (details)";
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"⚠️ Validation warnings/errors ({at})");
        sb.AppendLine($"Reason: {reason}");
        sb.AppendLine($"WARN={counts.Warnings}, ERR={counts.Errors}");
        if (counts.UnknownProviderWarnings > 0)
            sb.AppendLine($"- Unknown rooster provider ids: {counts.UnknownProviderWarnings}");
        if (counts.MissingRoosterKeyWarnings > 0)
            sb.AppendLine($"- Missing rooster FarmAnimal keys (optional mods/packs?): {counts.MissingRoosterKeyWarnings}");
        if (counts.SelectedEggErrors > 0)
            sb.AppendLine($"- EggKinds with invalid selection: {counts.SelectedEggErrors}");
        if (counts.InvalidRefErrors > 0)
            sb.AppendLine($"- Egg sources with invalid FarmAnimalRef: {counts.InvalidRefErrors}");
        if (counts.SelectedEggErrorSamples.Count > 0)
            sb.AppendLine($"  Examples: {string.Join("; ", counts.SelectedEggErrorSamples)}");
        if (counts.InvalidRefErrorSamples.Count > 0)
            sb.AppendLine($"  Bad refs: {string.Join("; ", counts.InvalidRefErrorSamples)}");
        if (counts.MissingRoosterKeySamples.Count > 0)
            sb.AppendLine($"  Missing rooster keys: {string.Join(", ", counts.MissingRoosterKeySamples)}");
        sb.AppendLine("Run: ra verify compat  (prints actionable fixes)");
        sb.AppendLine("Note: missing optional mods/packs are treated as WARN.");

        _compatValidationSummary = sb.ToString().TrimEnd();
    }

    private static string BuildCompatValidationLogSummary(string reason, CompatValidationCounts counts)
    {
        if (counts.Warnings == 0 && counts.Errors == 0)
            return $"Compat validation OK ({reason}).";

        var details = new List<string>();
        if (counts.UnknownProviderWarnings > 0)
            details.Add($"unknown-provider={counts.UnknownProviderWarnings}");
        if (counts.MissingRoosterKeyWarnings > 0)
            details.Add($"missing-rooster-key={counts.MissingRoosterKeyWarnings}");
        if (counts.SelectedEggErrors > 0)
            details.Add($"invalid-selected-egg={counts.SelectedEggErrors}");
        if (counts.InvalidRefErrors > 0)
            details.Add($"invalid-farmanimal-ref={counts.InvalidRefErrors}");
        if (counts.SelectedEggErrorSamples.Count > 0)
            details.Add($"sample-eggs=[{string.Join("; ", counts.SelectedEggErrorSamples)}]");
        if (counts.InvalidRefErrorSamples.Count > 0)
            details.Add($"sample-refs=[{string.Join("; ", counts.InvalidRefErrorSamples)}]");
        if (counts.MissingRoosterKeySamples.Count > 0)
            details.Add($"missing-rooster-keys=[{string.Join(", ", counts.MissingRoosterKeySamples)}]");

        var detailText = details.Count > 0
            ? $" Details: {string.Join(", ", details)}."
            : string.Empty;

        return
            $"Compat validation found issues ({reason}): WARN={counts.Warnings}, ERR={counts.Errors}.{detailText} " +
            "Run: ra verify compat";
    }


    private string BuildCompatStatusSummary()
    {
        return _compatSelection.BuildStatusReport(_saveState.Compat);
    }
}
