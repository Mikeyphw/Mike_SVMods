using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using StardewModdingAPI;
using WildEggsHatching.Application.AnimalNotes;
using WildEggsHatching.Application.Progression;
using WildEggsHatching.Infrastructure.Config;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using Mikew.BirdsOfAFeather.Helpers.SaveData;
using Mikew.BirdsOfAFeather.Helpers.Config;

namespace WildEggsHatching.Infrastructure.Diagnostics;

/// <summary>Concise startup health report for Phase 8 refactor.</summary>
public static class Phase8HealthReporter
{
    private static readonly Regex ImageSourceRegex = new(
        @"<image\s+source=""(?<src>[^""]+)""",
        RegexOptions.Compiled);

    public static void LogPhase8HealthReport(IModHelper helper, ISaveDataStore store, SmapiConfigProvider<ModConfig> config, ILog log)
    {
        try
        {
            var issues = new List<string>();

            var boafOk = helper.ModRegistry.IsLoaded("Mikew.birdsofafeather");
            if (!boafOk)
                issues.Add("BOAF missing: Mikew.birdsofafeather");

            issues.AddRange(CheckAssets(helper));
            issues.AddRange(CheckTmxImages(helper));
            issues.AddRange(CheckSchema(store));

            var configIssues = CheckConfig(config.Get()).ToList();
            issues.AddRange(configIssues);

            var summary =
                $"Phase8Health: " +
                $"boaf={(boafOk ? "ok" : "missing")} " +
                $"assets={(issues.Any(x => x.StartsWith("assets:", StringComparison.OrdinalIgnoreCase)) ? "bad" : "ok")} " +
                $"tmx={(issues.Any(x => x.StartsWith("tmx:", StringComparison.OrdinalIgnoreCase)) ? "bad" : "ok")} " +
                $"schema={(issues.Any(x => x.StartsWith("schema:", StringComparison.OrdinalIgnoreCase)) ? "bad" : "ok")} " +
                $"config={(configIssues.Count == 0 ? "ok" : "warn")}";

            log.Log(summary, issues.Count == 0 ? LogLevel.Info : LogLevel.Warn);

            foreach (var issue in issues.Distinct())
                log.Log($"Phase8Health - {issue}", LogLevel.Warn);
        }
        catch (Exception ex)
        {
            log.Log($"Phase8Health: failed to run health report: {ex}", LogLevel.Error);
        }
    }

    private static IEnumerable<string> CheckAssets(IModHelper helper)
    {
        var list = new List<string>();

        var assetsRoot = Path.Combine(helper.DirectoryPath, "assets");
        if (!Directory.Exists(assetsRoot))
        {
            list.Add("assets: missing /assets folder");
            return list;
        }

        var required = new[] { "objects", "bigcraftables", "mines" };
        foreach (var r in required)
        {
            var path = Path.Combine(assetsRoot, r);
            if (!Directory.Exists(path))
                list.Add($"assets: missing assets/{r}/");
        }

        // ensure lowercase folder names (Linux/macOS safety)
        foreach (var d in Directory.EnumerateDirectories(assetsRoot))
        {
            var name = Path.GetFileName(d);
            if (!string.Equals(name, name.ToLowerInvariant(), StringComparison.Ordinal))
                list.Add($"assets: non-lowercase folder '{name}'");
        }

        // ensure lowercase file names in critical folders
        foreach (var folder in required)
        {
            var dir = Path.Combine(assetsRoot, folder);
            if (!Directory.Exists(dir))
                continue;

            foreach (var f in Directory.EnumerateFiles(dir))
            {
                var name = Path.GetFileName(f);
                if (!string.Equals(name, name.ToLowerInvariant(), StringComparison.Ordinal))
                    list.Add($"assets: non-lowercase file '{folder}/{name}'");
            }
        }

        return list;
    }

    private static IEnumerable<string> CheckTmxImages(IModHelper helper)
    {
        var list = new List<string>();

        var minesDir = Path.Combine(helper.DirectoryPath, "assets", "mines");
        if (!Directory.Exists(minesDir))
            return list;

        foreach (var tmx in Directory.EnumerateFiles(minesDir, "*.tmx"))
        {
            var text = File.ReadAllText(tmx);

            foreach (Match m in ImageSourceRegex.Matches(text))
            {
                var src = m.Groups["src"].Value.Replace('\\', '/');
                var target = Path.Combine(minesDir, src);

                if (!File.Exists(target))
                    list.Add($"tmx: {Path.GetFileName(tmx)} references missing image '{src}'");

                if (!string.Equals(src, src.ToLowerInvariant(), StringComparison.Ordinal))
                    list.Add($"tmx: {Path.GetFileName(tmx)} image source not lowercase '{src}'");
            }
        }

        return list;
    }

    private static IEnumerable<string> CheckSchema(ISaveDataStore store)
    {
        var list = new List<string>();

        var p = store.ReadEnvelope<ProgressionSaveData>(WildEggsHatching.Application.Progression.SaveKeys.Progression);
        if (p is not null && p.Version != WildEggsHatching.Application.Progression.SaveKeys.Version)
            list.Add($"schema: Progression version {p.Version} != {WildEggsHatching.Application.Progression.SaveKeys.Version}");

        var a = store.ReadEnvelope<AnimalNotesSaveData>(WildEggsHatching.Application.AnimalNotes.SaveKeys.AnimalNotes);
        if (a is not null && a.Version != WildEggsHatching.Application.AnimalNotes.SaveKeys.Version)
            list.Add($"schema: AnimalNotes version {a.Version} != {WildEggsHatching.Application.AnimalNotes.SaveKeys.Version}");

        return list;
    }

    private static IEnumerable<string> CheckConfig(ModConfig c)
    {
        if (c.AnimalNotesWarnAfterDays < 1)
            yield return $"config: AnimalNotesWarnAfterDays={c.AnimalNotesWarnAfterDays} (should be >= 1)";

        if (c.AnimalNotesPlacementRadius < 1)
            yield return $"config: AnimalNotesPlacementRadius={c.AnimalNotesPlacementRadius} (should be >= 1)";
    }
}
