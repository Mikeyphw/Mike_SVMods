using System;
using System.Linq;
using StardewModdingAPI;
using RoosterControl.Application.Compat;
using Mikew.NestCore.Helpers.Logging;

using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;
using System.Collections.Generic;
using System.Text;
using StardewValley.GameData.FarmAnimals;

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
                "ra_compat usage (Phase 9B):\n" +
                "  ra_compat list\n" +
                "    - prints current compat catalogs and per-save selections.\n" +
                "  ra_compat cycle-egg <EggKind> [prev]\n" +
                "    - cycles selected egg source for the given EggKind.\n" +
                "  ra_compat cycle-rooster [prev]\n" +
                "    - cycles rooster provider in single-provider mode.\n" +
                "  ra_compat enable-all-roosters\n" +
                "    - enables all rooster providers (union mode).\n" +
                "  ra_compat validate [EggKind]\n" +
                "    - reports invalid FarmAnimalRef targets/indexes and shows resolved values.\n" +
                "\n" +
                "Notes:\n" +
                "  - Mutating commands are host-only.\n" +
                "  - Changes persist immediately to save data.",
                LogLevel.Info);
            return;
        }

        if (!Context.IsWorldReady)
        {
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                "ra_compat is only available after a save is loaded.",
                LogLevel.Info);
            return;
        }

        EnsureCompatInitialized();

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
            var kinds = string.Join(", ", Enum.GetNames(typeof(EggKind)));
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                $"Unknown EggKind '{args[1]}'. Valid values: {kinds}",
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
        : Enum.GetValues(typeof(EggKind)).Cast<EggKind>().ToArray();

    var sb = new StringBuilder();
    sb.AppendLine("ra_compat validate:");
    sb.AppendLine($"  FarmAnimals loaded: {animalData.Count}");
    sb.AppendLine($"  EggKinds: {string.Join(", ", kindsToCheck)}");

    var totalRefs = 0;
    var totalInvalid = 0;
    var selectedErrors = 0;

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

    sb.AppendLine();
    sb.AppendLine($"Summary: refsChecked={totalRefs}, invalidRefs={totalInvalid}, selectedErrors={selectedErrors}");

    var level = totalInvalid > 0 || selectedErrors > 0 ? LogLevel.Warn : LogLevel.Info;
    LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, sb.ToString(), level);
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
                    "Usage: ra_compat cycle-egg <EggKind> [prev]",
                    LogLevel.Info);
                return;
            }

            if (!Enum.TryParse<EggKind>(args[1], ignoreCase: true, out var kind))
            {
                var kinds = string.Join(", ", Enum.GetNames(typeof(EggKind)));
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    $"Unknown EggKind '{args[1]}'. Valid values: {kinds}",
                    LogLevel.Info);
                return;
            }

            var prev = args.Skip(2).Any(a => a.Equals("prev", StringComparison.OrdinalIgnoreCase));
            if (!_compatSelection.TryCycleEggSource(_saveState.Compat, kind, prev, out var selected, out var error))
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    error ?? "Failed to cycle egg source.",
                    LogLevel.Info);
                return;
            }

            PersistCompatSelectionChange();
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                selected is null
                    ? $"Egg source for '{kind}' updated."
                    : $"Egg source for '{kind}' -> '{selected.SourceId}' ({selected.DisplayName})",
                LogLevel.Info);
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
            if (!_compatSelection.TryCycleRoosterProviderSingle(_saveState.Compat, prev, out var selected, out var error))
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    error ?? "Failed to cycle rooster provider.",
                    LogLevel.Info);
                return;
            }

            PersistCompatSelectionChange();
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                selected is null
                    ? "Rooster provider updated."
                    : $"Rooster provider (single) -> '{selected.SourceId}' ({selected.DisplayName})",
                LogLevel.Info);
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

            if (!_compatSelection.EnableAllRoosterProviders(_saveState.Compat, out var enabledCount, out var error))
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                    error ?? "Failed to enable all rooster providers.",
                    LogLevel.Info);
                return;
            }

            PersistCompatSelectionChange();
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                $"Rooster providers (union) enabled: {enabledCount}",
                LogLevel.Info);
            return;
        }

        LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
            $"Unknown ra_compat verb '{verb}'. Use: ra_compat help",
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
}
