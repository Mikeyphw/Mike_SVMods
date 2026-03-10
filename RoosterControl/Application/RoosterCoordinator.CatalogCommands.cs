using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RoosterControl.Application.Catalogs;
using RoosterControl.Application.Catalogs.Console;
using RoosterControl.Framework.Config;
using StardewModdingAPI;
using Mikew.NestCore.Helpers.Logging;

using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace RoosterControl.Application;

internal sealed partial class RoosterCoordinator
{
    private const string RoostersCatalogPath = "catalogs/roosters.json";
    private const string ProduceCatalogPath = "catalogs/produce.json";
    private const string HatchingCatalogPath = "catalogs/hatching.json";

    private static readonly JsonSerializerOptions CanonicalJson = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };
    private void CatalogCommand(string command, string[] args)
    {
        var verb = args.FirstOrDefault()?.Trim() ?? "help";

        if (verb.Equals("help", StringComparison.OrdinalIgnoreCase) || verb.Equals("-h", StringComparison.OrdinalIgnoreCase))
        {
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                "ra catalog usage:\n" +
                "  ra catalog validate [--fix]\n" +
                "    - validates catalogs with per-file OK/MIGRATED/ERROR output.\n" +
                "    - --fix writes normalized JSON (with backups) and repairs invalid/missing files.\n" +
                "  ra catalog diff\n" +
                "    - writes a unified diff file showing what normalization would change.\n" +
                "  ra catalog export [snapshot]\n" +
                "    - writes current in-memory catalogs to disk (always backs up existing files).\n" +
                "    - snapshot writes to data/catalogs/exports/<timestamp>/ without overwriting.\n" +
                "  ra catalog reload\n" +
                "    - reload catalogs from disk, apply to config using current CatalogApplyMode, then invalidate assets.\n" +
                "  ra catalog mode [SeedOnly|RepairInvalid|MergeMissing|Authoritative]\n" +
                "    - prints or sets the catalog apply mode in config.json.",
                LogLevel.Info
            );
            return;
        }

        if (verb.Equals("mode", StringComparison.OrdinalIgnoreCase))
        {
            HandleCatalogMode(args.Skip(1).ToArray());
            return;
        }

        if (verb.Equals("validate", StringComparison.OrdinalIgnoreCase))
        {
            var fix = args.Any(a => a.Equals("fix", StringComparison.OrdinalIgnoreCase) || a.Equals("--fix", StringComparison.OrdinalIgnoreCase));
            HandleCatalogValidate(fix);
            return;
        }

        if (verb.Equals("diff", StringComparison.OrdinalIgnoreCase))
        {
            HandleCatalogDiff();
            return;
        }

        if (verb.Equals("export", StringComparison.OrdinalIgnoreCase))
        {
            var snapshot = args.Any(a => a.Equals("snapshot", StringComparison.OrdinalIgnoreCase));
            HandleCatalogExport(snapshot);
            return;
        }

        if (verb.Equals("reload", StringComparison.OrdinalIgnoreCase))
        {
            HandleCatalogReload();
            return;
        }

        LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, $"Unknown subcommand '{verb}'. Run: ra catalog help", LogLevel.Warn);
    }

    private void HandleCatalogMode(string[] args)
    {
        if (args.Length == 0)
        {
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, $"CatalogApplyMode = {Config.General.CatalogApplyMode}", LogLevel.Info);
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, "Valid modes: SeedOnly, RepairInvalid, MergeMissing, Authoritative", LogLevel.Info);
            return;
        }

        var raw = args[0].Trim();
        if (!Enum.TryParse<CatalogApplyMode>(raw, ignoreCase: true, out var mode))
        {
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, $"Unknown mode '{raw}'. Valid modes: SeedOnly, RepairInvalid, MergeMissing, Authoritative", LogLevel.Warn);
            return;
        }

        Config.General.CatalogApplyMode = mode;
        Helper.WriteConfig(Config);
        InvalidateDailyPlanCache("catalog mode changed");
        LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, $"CatalogApplyMode set to {mode}.", LogLevel.Info);
    }

    private void HandleCatalogValidate(bool fix)
    {
        var svc = new CatalogCommandService(Helper.DirectoryPath);
        var result = svc.ValidateAndMaybeFix(fix);

        LogCatalogReports("Catalog validation results", result.Reports);

        if (!fix)
            return;

        if (!string.IsNullOrWhiteSpace(result.BackupRelativeDir))
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, $"Wrote normalized catalogs. Backups: {result.BackupRelativeDir}/", LogLevel.Info);

        if (result.PostFixIssues.Count > 0)
        {
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, "Catalogs still have validation errors after --fix:", LogLevel.Error);
            foreach (var issue in result.PostFixIssues)
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, $"  - {issue}", LogLevel.Error);
        }
    }

    private void HandleCatalogDiff()
    {
        var defaults = CatalogDefaults.CreateDefaults();
        var (bundle, _) = LoadBundleForCommands(defaults, repairInvalidFiles: false);
        CatalogSanitizer.Sanitize(bundle);

        var timestamp = DateTimeOffset.Now.ToString("yyyyMMddHHmmss");
        var diffDir = EnsureDir(Path.Combine(Helper.DirectoryPath, "data", "catalogs", "diffs"));
        var files = new[]
        {
            (Name: "roosters", Rel: RoostersCatalogPath, Obj: (object)bundle.Roosters),
            (Name: "produce", Rel: ProduceCatalogPath, Obj: (object)bundle.Produce),
            (Name: "hatching", Rel: HatchingCatalogPath, Obj: (object)bundle.Hatching),
        };

        foreach (var f in files)
        {
            var oldText = SafeReadText(GetDataPath(f.Rel));
            var newText = SerializeCanonical(f.Obj);

            if (NormalizeLineEndings(oldText) == NormalizeLineEndings(newText))
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, $"diff {f.Name}: no changes", LogLevel.Info);
                continue;
            }

            var diffText = BuildLineDiff(oldText, newText, label: f.Rel);
            var outPath = Path.Combine(diffDir, $"{timestamp}_{f.Name}.diff.txt");
            File.WriteAllText(outPath, diffText, Encoding.UTF8);
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, $"diff {f.Name}: wrote data/catalogs/diffs/{Path.GetFileName(outPath)}", LogLevel.Info);
        }
    }

    private void HandleCatalogExport(bool snapshot)
    {
        var timestamp = DateTimeOffset.Now.ToString("yyyyMMddHHmmss");

        if (snapshot)
        {
            var exportRoot = EnsureDir(Path.Combine(Helper.DirectoryPath, "data", "catalogs", "exports", timestamp));
            WriteToSnapshot(exportRoot, "roosters.json", _catalogs.Roosters);
            WriteToSnapshot(exportRoot, "produce.json", _catalogs.Produce);
            WriteToSnapshot(exportRoot, "hatching.json", _catalogs.Hatching);

            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, $"Wrote catalog snapshot: data/catalogs/exports/{timestamp}/", LogLevel.Info);
            return;
        }

        var backupRoot = Path.Combine(Helper.DirectoryPath, "data", "catalogs", "backups", timestamp);
        WriteWithBackup(RoostersCatalogPath, _catalogs.Roosters, backupRoot);
        WriteWithBackup(ProduceCatalogPath, _catalogs.Produce, backupRoot);
        WriteWithBackup(HatchingCatalogPath, _catalogs.Hatching, backupRoot);

        LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, $"Exported catalogs to data/catalogs/*. Backups: data/catalogs/backups/{timestamp}/", LogLevel.Info);
    }

    private void HandleCatalogReload()
    {
        if (!Context.IsWorldReady)
        {
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, "Save not loaded; reload requires a loaded save (so assets can be invalidated).", LogLevel.Info);
            return;
        }

        if (!Context.IsMainPlayer)
        {
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, "Only the host can reload catalogs.", LogLevel.Warn);
            return;
        }

        var defaults = CatalogDefaults.CreateDefaults();

        CatalogBundle bundle;
        try
        {
            var (loaded, _) = LoadBundleForCommands(defaults, repairInvalidFiles: false);
            CatalogSanitizer.Sanitize(loaded);

            var issues = CatalogValidator.Validate(loaded);
            if (issues.Count > 0)
            {
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, "Catalog validation errors (reload aborted):", LogLevel.Error);
                foreach (var issue in issues)
                    LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, $"  - {issue}", LogLevel.Error);
                return;
            }

            bundle = loaded;
        }
        catch (Exception ex)
        {
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, $"Catalog reload failed: {ex}", LogLevel.Error);
            return;
        }

        var changed = _catalogManager.ApplyToConfig(
            config: Config,
            catalogs: bundle,
            isNewConfig: false,
            mode: Config.General.CatalogApplyMode,
            monitor: Monitor
        );

        if (changed)
            Helper.WriteConfig(Config);

        _catalogs = bundle;
        RoosterHelper = new Helpers.RoosterHelper(Config, Helper, Monitor, _catalogs.Roosters, _catalogs.Produce);

        ResetAssetEditorsForCatalogReload();
        InvalidateDailyPlanCache("catalog reload");
        RequestInvalidate(FarmAnimalsAssetName);
        RequestInvalidate(ObjectsAssetName);

        LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, "Catalogs reloaded; config updated (if needed) and assets invalidated.", LogLevel.Info);
    }

    private void ResetAssetEditorsForCatalogReload()
    {
        _farmAnimalsEditor = null;
        _objectsEditor = null;
        _fertilizedEggsEditor = null;
    }

    private static void WriteToSnapshot<T>(string exportRoot, string fileName, T value)
    {
        var path = Path.Combine(exportRoot, fileName);
        var json = JsonSerializer.Serialize(value, CanonicalJson);
        File.WriteAllText(path, json, Encoding.UTF8);
    }

    private (CatalogBundle Bundle, IReadOnlyList<CatalogFileReport> LoadReports) LoadBundleForCommands(
        CatalogBundle defaults,
        bool repairInvalidFiles
    )
    {
        var reports = new List<CatalogFileReport>();

        var roosters = ReadOrDefault(RoostersCatalogPath, defaults.Roosters, "roosters", reports, repairInvalidFiles);
        var produce = ReadOrDefault(ProduceCatalogPath, defaults.Produce, "produce", reports, repairInvalidFiles);
        var hatching = ReadOrDefault(HatchingCatalogPath, defaults.Hatching, "hatching", reports, repairInvalidFiles);

        var bundle = new CatalogBundle { Roosters = roosters, Produce = produce, Hatching = hatching };
        return (bundle, reports);
    }

    private T ReadOrDefault<T>(
        string relativePath,
        T defaultValue,
        string name,
        List<CatalogFileReport> reports,
        bool repairInvalidFiles
    ) where T : class, new()
    {
        try
        {
            var loaded = Helper.Data.ReadJsonFile<T>(relativePath);

            if (loaded is null)
            {
                reports.Add(new CatalogFileReport(name, relativePath, CatalogFileStatus.Migrated, WouldChange: true, Issues: new[] { "File missing; would create defaults." }));
                return defaultValue;
            }

            return loaded;
        }
        catch (Exception ex)
        {
            if (!repairInvalidFiles)
            {
                reports.Add(new CatalogFileReport(name, relativePath, CatalogFileStatus.Error, WouldChange: false, Issues: new[] { $"Failed to read JSON: {ex.Message}" }));
                return defaultValue;
            }

            var timestamp = DateTimeOffset.Now.ToString("yyyyMMddHHmmss");
            var badFilePath = GetDataPath(relativePath);
            var backupRoot = EnsureDir(Path.Combine(Helper.DirectoryPath, "data", "catalogs", "invalid", timestamp));
            var backupPath = Path.Combine(backupRoot, Path.GetFileName(relativePath));

            try
            {
                if (File.Exists(badFilePath))
                    File.Copy(badFilePath, backupPath, overwrite: true);
            }
            catch
            {
                // best-effort
            }

            reports.Add(new CatalogFileReport(name, relativePath, CatalogFileStatus.Migrated, WouldChange: true, Issues: new[] { $"Invalid JSON backed up to data/catalogs/invalid/{timestamp}/{Path.GetFileName(relativePath)}; defaults will be written on --fix." }));
            return defaultValue;
        }
    }

    private IReadOnlyList<CatalogFileReport> BuildPerFileReports(
        CatalogBundle sanitized,
        CatalogBundle defaults,
        IReadOnlyList<CatalogFileReport> loadReports,
        IReadOnlyList<CatalogIssue> issues
    )
    {
        var issuesByCatalog = issues
            .GroupBy(i => i.Catalog, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(i => i.ToString()).ToList(), StringComparer.OrdinalIgnoreCase);

        var files = new[]
        {
            ("roosters", RoostersCatalogPath, (object)sanitized.Roosters, (object)defaults.Roosters),
            ("produce", ProduceCatalogPath, (object)sanitized.Produce, (object)defaults.Produce),
            ("hatching", HatchingCatalogPath, (object)sanitized.Hatching, (object)defaults.Hatching),
        };

        var results = new List<CatalogFileReport>();

        foreach (var (name, rel, obj, defObj) in files)
        {
            var load = loadReports.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            var wouldChange = load?.WouldChange ?? false;
            var loadIssues = load?.Issues ?? Array.Empty<string>();

            if (!wouldChange && File.Exists(GetDataPath(rel)))
            {
                var oldText = SafeReadText(GetDataPath(rel));
                var newText = SerializeCanonical(obj);
                wouldChange = NormalizeLineEndings(oldText) != NormalizeLineEndings(newText);
            }

            var catalogIssues = issuesByCatalog.TryGetValue(name, out var list)
                ? list
                : Array.Empty<string>();

            CatalogFileStatus status;
            var combinedIssues = new List<string>();
            combinedIssues.AddRange(loadIssues);
            combinedIssues.AddRange(catalogIssues);

            if (load?.Status == CatalogFileStatus.Error)
                status = CatalogFileStatus.Error;
            else if (catalogIssues.Count > 0)
                status = CatalogFileStatus.Error;
            else if (wouldChange || (load?.Status == CatalogFileStatus.Migrated))
                status = CatalogFileStatus.Migrated;
            else
                status = CatalogFileStatus.Ok;

            results.Add(new CatalogFileReport(name, rel, status, wouldChange, combinedIssues));
        }

        return results;
    }

    private void LogCatalogReports(string title, IReadOnlyList<CatalogFileReport> reports)
    {
        LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, $"{title}:", LogLevel.Info);

        foreach (var r in reports)
        {
            var label = r.Status switch
            {
                CatalogFileStatus.Ok => "OK",
                CatalogFileStatus.Migrated => "MIGRATED",
                _ => "ERROR"
            };

            var change = r.WouldChange ? " (would change)" : string.Empty;
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, $"  {r.Name}: {label}{change}", r.Status == CatalogFileStatus.Error ? LogLevel.Error : LogLevel.Info);

            if (r.Issues.Count > 0 && r.Status == CatalogFileStatus.Error)
            {
                foreach (var issue in r.Issues.Take(25))
                    LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, $"    - {issue}", LogLevel.Error);
                if (r.Issues.Count > 25)
                    LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, $"    ... and {r.Issues.Count - 25} more", LogLevel.Error);
            }
            else if (r.Issues.Count > 0 && r.Status == CatalogFileStatus.Migrated)
            {
                foreach (var issue in r.Issues.Take(5))
                    LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, $"    - {issue}", LogLevel.Info);
                if (r.Issues.Count > 5)
                    LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, $"    ... and {r.Issues.Count - 5} more", LogLevel.Info);
            }
        }
    }

    private void WriteWithBackup<T>(string relativePath, T value, string backupRoot)
        where T : class
    {
        if (value is null)
            return;

        var fullPath = GetDataPath(relativePath);
        EnsureDir(Path.GetDirectoryName(fullPath) ?? string.Empty);
        EnsureDir(backupRoot);

        if (File.Exists(fullPath))
        {
            var backupPath = Path.Combine(backupRoot, Path.GetFileName(fullPath));
            try
            {
                File.Copy(fullPath, backupPath, overwrite: true);
            }
            catch
            {
                // best-effort
            }
        }

        Helper.Data.WriteJsonFile(relativePath, value);
    }

    private string GetDataPath(string relativePath)
    {
        var parts = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(Helper.DirectoryPath, "data", parts);
    }

    private static string EnsureDir(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && !Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }

    private static string SafeReadText(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string SerializeCanonical(object obj)
        => JsonSerializer.Serialize(obj, CanonicalJson);

    private static string NormalizeLineEndings(string text)
        => (text ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");

    private static string BuildLineDiff(string oldText, string newText, string label)
    {
        var oldLines = NormalizeLineEndings(oldText).Split('\n');
        var newLines = NormalizeLineEndings(newText).Split('\n');

        var dp = new int[oldLines.Length + 1, newLines.Length + 1];

        for (var i = oldLines.Length - 1; i >= 0; i--)
        for (var j = newLines.Length - 1; j >= 0; j--)
            dp[i, j] = oldLines[i] == newLines[j]
                ? dp[i + 1, j + 1] + 1
                : Math.Max(dp[i + 1, j], dp[i, j + 1]);

        var sb = new StringBuilder();
        sb.AppendLine($"--- {label} (disk)");
        sb.AppendLine($"+++ {label} (normalized)");
        sb.AppendLine();

        var a = 0;
        var b = 0;

        while (a < oldLines.Length && b < newLines.Length)
        {
            if (oldLines[a] == newLines[b])
            {
                sb.AppendLine($" {oldLines[a]}");
                a++;
                b++;
                continue;
            }

            if (dp[a + 1, b] >= dp[a, b + 1])
            {
                sb.AppendLine($"-{oldLines[a]}");
                a++;
            }
            else
            {
                sb.AppendLine($"+{newLines[b]}");
                b++;
            }
        }

        while (a < oldLines.Length)
        {
            sb.AppendLine($"-{oldLines[a]}");
            a++;
        }

        while (b < newLines.Length)
        {
            sb.AppendLine($"+{newLines[b]}");
            b++;
        }

        return sb.ToString();
    }
}
