using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RoosterAddon.Application.Catalogs;

namespace RoosterAddon.Application.Catalogs.Console;

/// <summary>
/// Implements the catalog console commands in a testable, file-based way.
/// </summary>
internal sealed class CatalogCommandService
{
    internal const string RoostersCatalogPath = "catalogs/roosters.json";
    internal const string ProduceCatalogPath = "catalogs/produce.json";
    internal const string HatchingCatalogPath = "catalogs/hatching.json";

    private static readonly JsonSerializerOptions CanonicalJson = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly JsonSerializerOptions ReadJson = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _modDirectoryPath;
    private readonly Func<DateTimeOffset> _now;

    internal CatalogCommandService(string modDirectoryPath, Func<DateTimeOffset>? now = null)
    {
        _modDirectoryPath = modDirectoryPath ?? throw new ArgumentNullException(nameof(modDirectoryPath));
        _now = now ?? (() => DateTimeOffset.Now);
    }

    internal CatalogValidateResult ValidateAndMaybeFix(bool fix)
    {
        var defaults = CatalogDefaults.CreateDefaults();

        var timestamp = _now().ToString("yyyyMMddHHmmss");
        var invalidRelDir = $"data/catalogs/invalid/{timestamp}";
        var backupRelDir = fix ? $"data/catalogs/backups/{timestamp}" : null;

        var loadReports = new List<CatalogFileReport>();

        var roosters = ReadOrDefault(RoostersCatalogPath, defaults.Roosters, "roosters", loadReports, repairInvalidFiles: fix, invalidRelDir);
        var produce = ReadOrDefault(ProduceCatalogPath, defaults.Produce, "produce", loadReports, repairInvalidFiles: fix, invalidRelDir);
        var hatching = ReadOrDefault(HatchingCatalogPath, defaults.Hatching, "hatching", loadReports, repairInvalidFiles: fix, invalidRelDir);

        var bundle = new CatalogBundle { Roosters = roosters, Produce = produce, Hatching = hatching };
        CatalogSanitizer.Sanitize(bundle);

        var issues = CatalogValidator.Validate(bundle).ToList();
        var reports = BuildPerFileReports(bundle, defaults, loadReports, issues);

        if (!fix)
            return new CatalogValidateResult(reports, BackupRelativeDir: null, InvalidRelativeDir: null, Issues: issues, PostFixIssues: Array.Empty<CatalogIssue>());

        var backupRoot = GetFullPath(backupRelDir!);
        WriteWithBackup(RoostersCatalogPath, bundle.Roosters, backupRoot);
        WriteWithBackup(ProduceCatalogPath, bundle.Produce, backupRoot);
        WriteWithBackup(HatchingCatalogPath, bundle.Hatching, backupRoot);

        var postIssues = CatalogValidator.Validate(bundle).ToList();
        return new CatalogValidateResult(reports, backupRelDir, invalidRelDir, issues, postIssues);
    }

    private T ReadOrDefault<T>(
        string relativePath,
        T defaultValue,
        string name,
        List<CatalogFileReport> reports,
        bool repairInvalidFiles,
        string invalidRelDir
    ) where T : class, new()
    {
        var fullPath = GetDataPath(relativePath);

        if (!File.Exists(fullPath))
        {
            reports.Add(new CatalogFileReport(
                name,
                relativePath,
                CatalogFileStatus.Migrated,
                WouldChange: true,
                Issues: new[] { "File missing; would create defaults." }
            ));
            return defaultValue;
        }

        try
        {
            var json = File.ReadAllText(fullPath, Encoding.UTF8);
            var loaded = JsonSerializer.Deserialize<T>(json, ReadJson);

            if (loaded is null)
            {
                reports.Add(new CatalogFileReport(
                    name,
                    relativePath,
                    CatalogFileStatus.Migrated,
                    WouldChange: true,
                    Issues: new[] { "File empty or invalid; would create defaults." }
                ));
                return defaultValue;
            }

            return loaded;
        }
        catch (Exception ex)
        {
            if (!repairInvalidFiles)
            {
                reports.Add(new CatalogFileReport(
                    name,
                    relativePath,
                    CatalogFileStatus.Error,
                    WouldChange: false,
                    Issues: new[] { $"Failed to read JSON: {ex.Message}" }
                ));
                return defaultValue;
            }

            var invalidRoot = EnsureDir(GetFullPath(invalidRelDir));
            var backupPath = Path.Combine(invalidRoot, Path.GetFileName(relativePath));

            try
            {
                File.Copy(fullPath, backupPath, overwrite: true);
            }
            catch
            {
                // best-effort
            }

            reports.Add(new CatalogFileReport(
                name,
                relativePath,
                CatalogFileStatus.Migrated,
                WouldChange: true,
                Issues: new[]
                {
                    $"Invalid JSON backed up to {invalidRelDir}/{Path.GetFileName(relativePath)}; defaults will be written on --fix."
                }
            ));
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
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.Select(i => i.ToString()).ToList(),
                StringComparer.OrdinalIgnoreCase
            );

        var files = new[]
        {
            ("roosters", RoostersCatalogPath, (object)sanitized.Roosters, (object)defaults.Roosters),
            ("produce", ProduceCatalogPath, (object)sanitized.Produce, (object)defaults.Produce),
            ("hatching", HatchingCatalogPath, (object)sanitized.Hatching, (object)defaults.Hatching),
        };

        var results = new List<CatalogFileReport>();

        foreach (var (name, rel, obj, _) in files)
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

    private void WriteWithBackup<T>(string relativePath, T value, string backupRoot)
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

        var json = SerializeCanonical(value);
        File.WriteAllText(fullPath, json, Encoding.UTF8);
    }

    private static string SerializeCanonical(object obj)
        => JsonSerializer.Serialize(obj, CanonicalJson);

    private static string SafeReadText(string path)
    {
        try { return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : string.Empty; }
        catch { return string.Empty; }
    }

    private static string NormalizeLineEndings(string text)
        => (text ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");

    private string GetDataPath(string relativeDataPath)
        => Path.Combine(_modDirectoryPath, "data", relativeDataPath.Replace('/', Path.DirectorySeparatorChar));

    private string GetFullPath(string relativePath)
        => Path.Combine(_modDirectoryPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static string EnsureDir(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }
}
