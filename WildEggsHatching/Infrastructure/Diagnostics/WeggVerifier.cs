using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using WildEggsHatching.Application.AnimalNotes;
using WildEggsHatching.Application.Progression;
using WildEggsHatching.Infrastructure.Get();
using Mikew.BirdsOfAFeather.Helpers.Logging;
using WildEggsHatching.Infrastructure.ModData;
using Mikew.BirdsOfAFeather.Helpers.SaveData;
using WildEggsHatching.Application.Animals;
using WildEggsHatching.Infrastructure.ModData;
using WildEggsHatching.Infrastructure.ModData.Models;
using xTile;
using Mikew.BirdsOfAFeather.Helpers.Get();
using WildEggsHatching.Infrastructure.Diagnostics;

namespace WildEggsHatching.Infrastructure.Diagnostics;

public sealed record WeggVerifyOptions(
    bool AsJson,
    bool Full,
    bool IsWorldReady,
    bool IsMainPlayer,
    ProgressionStep ProgressionStep
);

public sealed record WeggVerifyResult(bool Success, string Rendered, WeggVerifyReport Report);

public enum WeggVerifyStatus
{
    Pass = 0,
    Warn = 1,
    Fail = 2,
}

public sealed record WeggVerifyCheck(string Id, WeggVerifyStatus Status, string Message);

public sealed class WeggVerifyReport
{
    public List<WeggVerifyCheck> Checks { get; } = new();

    public int PassCount => Checks.Count(x => x.Status == WeggVerifyStatus.Pass);
    public int WarnCount => Checks.Count(x => x.Status == WeggVerifyStatus.Warn);
    public int FailCount => Checks.Count(x => x.Status == WeggVerifyStatus.Fail);

    public bool Success => FailCount == 0;

    public void Add(string id, WeggVerifyStatus status, string message) => Checks.Add(new WeggVerifyCheck(id, status, message));
}

public sealed class WeggVerifier
{
    private static readonly Regex ImageSourceRegex = new("<image\\s+source=\"(?<src>[^\"]+)\"", RegexOptions.Compiled);

    private readonly IModHelper _helper;
    private readonly ISaveDataStore _store;
    private readonly SmapiConfigProvider<ModConfig> _config;
    private readonly IModDataLoader _dataLoader;
    private readonly ILog _log;

    public WeggVerifier(IModHelper helper, ISaveDataStore store, SmapiConfigProvider<ModConfig> config, IModDataLoader dataLoader, ILog log)
    {
        _helper = helper ?? throw new ArgumentNullException(nameof(helper));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public WeggVerifyResult Run(WeggVerifyOptions options)
    {
        var report = new WeggVerifyReport();

        AddDependencyChecks(report);
        AddLocalDuplicateChecks(report);
        AddAssetFilesystemChecks(report);
        AddTmxChecks(report);

        AddConfigChecks(report);

        if (options.Full)
        {
            AddSchemaChecks(report);
        }
        else
        {
            report.Add("schema", WeggVerifyStatus.Warn, "Schema checks skipped (use --full on host).");
        }

        AddAnimalNotesValidation(report, options);

        if (options.IsWorldReady)
        {
            AddRuntimeProbeChecks(report);
        }
        else
        {
            report.Add("probe/world", WeggVerifyStatus.Warn, "World not ready; runtime asset probes skipped.");
        }

        var rendered = options.AsJson ? RenderJson(report) : RenderText(report);
        return new WeggVerifyResult(report.Success, rendered, report);
    }

    private void AddDependencyChecks(WeggVerifyReport report)
    {
        var boafOk = _helper.ModRegistry.IsLoaded("Mikew.birdsofafeather");
        var cpOk = _helper.ModRegistry.IsLoaded("Pathoschild.ContentPatcher");
        report.Add("dep/contentpatcher", cpOk ? WeggVerifyStatus.Pass : WeggVerifyStatus.Fail,
            cpOk ? "Content Patcher loaded (Pathoschild.ContentPatcher)." : "Missing required dependency Content Patcher (Pathoschild.ContentPatcher).");

        var forageOk = _helper.ModRegistry.IsLoaded("yourdorkbrains.cp.wildeggsforage");
        report.Add("dep/wildeggsforage", forageOk ? WeggVerifyStatus.Pass : WeggVerifyStatus.Fail,
            forageOk ? "Forage content pack loaded (yourdorkbrains.cp.wildeggsforage)." : "Missing required content pack yourdorkbrains.cp.wildeggsforage.");

        report.Add("dep/boaf", boafOk ? WeggVerifyStatus.Pass : WeggVerifyStatus.Fail,
            boafOk ? "BOAF loaded (Mikew.birdsofafeather)." : "Missing required dependency BOAF (Mikew.birdsofafeather).");
    }

    private void AddLocalDuplicateChecks(WeggVerifyReport report)
    {
        try
        {
            var localDll = Path.Combine(_helper.DirectoryPath, "BirdsOfAFeather.dll");
            var localFolder = Path.Combine(_helper.DirectoryPath, "BirdsOfAFeather");
            var found = File.Exists(localDll) || Directory.Exists(localFolder);

            report.Add("dep/boaf-dup", found ? WeggVerifyStatus.Fail : WeggVerifyStatus.Pass,
                found ? "Local BOAF files detected inside WildEggsHatching folder (remove them; BOAF must be its own mod)." : "No local BOAF duplicates detected.");
        }
        catch (Exception ex)
        {
            report.Add("dep/boaf-dup", WeggVerifyStatus.Warn, $"Duplicate check failed (best-effort): {ex.GetType().Name}");
        }
    }

    private void AddAssetFilesystemChecks(WeggVerifyReport report)
    {
        var assetsRoot = Path.Combine(_helper.DirectoryPath, "assets");
        if (!Directory.Exists(assetsRoot))
        {
            report.Add("assets/root", WeggVerifyStatus.Fail, "Missing /assets folder.");
            return;
        }

        var required = new[] { "objects", "bigcraftables", "mines" };
        foreach (var r in required)
        {
            var path = Path.Combine(assetsRoot, r);
            report.Add($"assets/dir/{r}", Directory.Exists(path) ? WeggVerifyStatus.Pass : WeggVerifyStatus.Fail, Directory.Exists(path) ? $"assets/{r}/ present." : $"Missing assets/{r}/");
        }

        foreach (var d in Directory.EnumerateDirectories(assetsRoot))
        {
            var name = Path.GetFileName(d);
            if (!string.Equals(name, name.ToLowerInvariant(), StringComparison.Ordinal))
                report.Add("assets/case/dir", WeggVerifyStatus.Warn, $"Non-lowercase assets folder '{name}' (Linux/macOS risk).");
        }

        foreach (var folder in required)
        {
            var dir = Path.Combine(assetsRoot, folder);
            if (!Directory.Exists(dir))
                continue;

            foreach (var f in Directory.EnumerateFiles(dir))
            {
                var name = Path.GetFileName(f);
                if (!string.Equals(name, name.ToLowerInvariant(), StringComparison.Ordinal))
                    report.Add("assets/case/file", WeggVerifyStatus.Warn, $"Non-lowercase file '{folder}/{name}' (Linux/macOS risk).");
            }
        }
    }

    private void AddTmxChecks(WeggVerifyReport report)
    {
        var minesDir = Path.Combine(_helper.DirectoryPath, "assets", "mines");
        if (!Directory.Exists(minesDir))
            return;

        foreach (var tmx in Directory.EnumerateFiles(minesDir, "*.tmx"))
        {
            var text = File.ReadAllText(tmx);

            foreach (Match m in ImageSourceRegex.Matches(text))
            {
                var src = m.Groups["src"].Value.Replace('\\', '/');
                var target = Path.Combine(minesDir, src);

                if (!File.Exists(target))
                    report.Add("tmx/missing-image", WeggVerifyStatus.Fail, $"{Path.GetFileName(tmx)} references missing image '{src}'.");

                if (!string.Equals(src, src.ToLowerInvariant(), StringComparison.Ordinal))
                    report.Add("tmx/case-image", WeggVerifyStatus.Warn, $"{Path.GetFileName(tmx)} image source not lowercase '{src}'.");
            }
        }
    }

    private void AddSchemaChecks(WeggVerifyReport report)
    {
        try
        {
            var p = _store.ReadEnvelope<ProgressionSaveData>(SaveKeys.Progression);
            if (p is null)
            {
                report.Add("schema/progression", WeggVerifyStatus.Pass, "Progression schema: no save data yet.");
            }
            else if (p.Version != SaveKeys.Version)
            {
                report.Add("schema/progression", WeggVerifyStatus.Fail, $"Progression schema version {p.Version} != {SaveKeys.Version}.");
            }
            else
            {
                report.Add("schema/progression", WeggVerifyStatus.Pass, $"Progression schema version {p.Version} OK.");
            }

            var a = _store.ReadEnvelope<AnimalNotesSaveData>(Application.AnimalNotes.SaveKeys.AnimalNotes);
            if (a is null)
            {
                report.Add("schema/animalnotes", WeggVerifyStatus.Pass, "AnimalNotes schema: no save data yet.");
            }
            else if (a.Version != Application.AnimalNotes.SaveKeys.Version)
            {
                report.Add("schema/animalnotes", WeggVerifyStatus.Fail, $"AnimalNotes schema version {a.Version} != {Application.AnimalNotes.SaveKeys.Version}.");
            }
            else
            {
                report.Add("schema/animalnotes", WeggVerifyStatus.Pass, $"AnimalNotes schema version {a.Version} OK.");
            }
        }
        catch (Exception ex)
        {
            report.Add("schema", WeggVerifyStatus.Fail, $"Schema read failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void AddConfigChecks(WeggVerifyReport report)
    {
        var c = _config.Get();
        if (c.AnimalNotesWarnAfterDays < 1)
            report.Add("config/animalnotes-warn", WeggVerifyStatus.Warn, $"AnimalNotesWarnAfterDays={c.AnimalNotesWarnAfterDays} (should be >= 1).");
        else
            report.Add("config/animalnotes-warn", WeggVerifyStatus.Pass, $"AnimalNotesWarnAfterDays={c.AnimalNotesWarnAfterDays}.");

        if (c.AnimalNotesPlacementRadius < 1)
            report.Add("config/animalnotes-radius", WeggVerifyStatus.Warn, $"AnimalNotesPlacementRadius={c.AnimalNotesPlacementRadius} (should be >= 1).");
        else
            report.Add("config/animalnotes-radius", WeggVerifyStatus.Pass, $"AnimalNotesPlacementRadius={c.AnimalNotesPlacementRadius}.");
    }

    private void AddAnimalNotesValidation(WeggVerifyReport report, WeggVerifyOptions options)
    {
        if (!options.IsWorldReady)
        {
            report.Add("animalnotes/validate", WeggVerifyStatus.Warn, "World not ready; AnimalNotes validation skipped.");
            return;
        }

        if (options.ProgressionStep < ProgressionStep.PartVI)
        {
            report.Add("animalnotes/validate", WeggVerifyStatus.Warn, $"Progression step is {options.ProgressionStep}; AnimalNotes validation normally starts at PartVI.");
            return;
        }

        try
        {
            var snapshot = _dataLoader.Load();
            var expectedTokens = AnimalNotesValidationRules.GetExpectedAnimalTokens(
                wildAnimalKeys: snapshot.WildAnimals.Keys,
                farmAnimalKeys: Game1.farmAnimalData.Keys);

            var objects = SafeLoad<Dictionary<string, StardewValley.GameData.Objects.ObjectData>>(Infrastructure.Content.GameAssets.Objects) ?? new();
            var quests = SafeLoad<Dictionary<string, string>>(Infrastructure.Content.GameAssets.Quests) ?? new();
            var mail = SafeLoad<Dictionary<string, string>>(Infrastructure.Content.GameAssets.Mail) ?? new();

            var result = AnimalNotesValidationRules.Analyze(
                expectedTokens: expectedTokens,
                presentNoteIds: objects.Keys,
                presentQuestIds: quests.Keys,
                presentMailIds: mail.Keys);

            if (result.MissingNotes.Count == 0 && result.MissingQuests.Count == 0 && result.MissingMail.Count == 0 &&
                result.ExtraNotes.Count == 0 && result.ExtraQuests.Count == 0 && result.ExtraMail.Count == 0)
            {
                report.Add("animalnotes/validate", WeggVerifyStatus.Pass, $"AnimalNotes validation OK (animals={expectedTokens.Count}).");
            }
            else
            {
                report.Add("animalnotes/validate", WeggVerifyStatus.Fail,
                    $"AnimalNotes mismatch: animals={expectedTokens.Count} missing(notes={result.MissingNotes.Count}, quests={result.MissingQuests.Count}, mail={result.MissingMail.Count}) extra(notes={result.ExtraNotes.Count}, quests={result.ExtraQuests.Count}, mail={result.ExtraMail.Count}).");
            }
        }
        catch (Exception ex)
        {
            report.Add("animalnotes/validate", WeggVerifyStatus.Fail, $"AnimalNotes validation failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void AddRuntimeProbeChecks(WeggVerifyReport report)
    {
        // These prove BOAF hooks are wired: game content must be able to load a TMX and a couple PNGs.
        TryProbe<Map>(report, "probe/map/tomb", "Maps/Mines/Level77377");
        TryProbe<Texture2D>(report, "probe/tex/tombtilesheet", "Maps/Mines/Z_TombTilesheet");
        TryProbe<Texture2D>(report, "probe/tex/tracker", "Maps/Mines/tracker");
        TryProbe<Texture2D>(report, "probe/tex/object-note", $"Mods/{Constants.ModId}/objects/note");

        // Content pack validation (requires world-ready).
        var cpOk = _helper.ModRegistry.IsLoaded("Pathoschild.ContentPatcher");
        var packOk = _helper.ModRegistry.IsLoaded("yourdorkbrains.cp.wildeggsforage");
        if (cpOk && packOk)
        {
            try
            {
                // Best-effort: verify at least one object is tagged as a WE egg.
                var objects = SafeLoad<Dictionary<string, ObjectData>>(WildEggsHatching.Infrastructure.Content.GameAssets.Objects);
                var tagged = objects?.Values.Count(o => o.ContextTags?.Contains("wegg_item") == true) ?? 0;
                report.Add("cp/objects", tagged > 0 ? WeggVerifyStatus.Pass : WeggVerifyStatus.Fail,
                    tagged > 0 ? $"Found {tagged} wegg_item objects." : "No wegg_item objects found in Data/Objects (content edits not applied).");

                // Best-effort: verify incubator recipe exists (string match).
                var recipes = SafeLoad<Dictionary<string, string>>(WildEggsHatching.Infrastructure.Content.GameAssets.CraftingRecipes);
                var hasRecipe = recipes?.Keys.Any(k => k.Contains("Wild Egg Incubator", StringComparison.OrdinalIgnoreCase)) == true;
                report.Add("cp/recipes", hasRecipe ? WeggVerifyStatus.Pass : WeggVerifyStatus.Warn,
                    hasRecipe ? "Incubator recipe key present." : "Incubator recipe key not found (if expected, content edits may be missing).");

                // Best-effort: ask Content Patcher for patch list (reflection; may fail depending on CP version).
                try
                {
                    var cpApi = _helper.ModRegistry.GetApi<object>("Pathoschild.ContentPatcher");
                    if (cpApi is not null)
                    {
                        var mi = cpApi.GetType().GetMethod("GetPatches", Type.EmptyTypes);
                        if (mi is not null)
                        {
                            var patches = mi.Invoke(cpApi, null) as System.Collections.IEnumerable;
                            var found = false;
                            if (patches is not null)
                            {
                                foreach (var p in patches)
                                {
                                    var ownerProp = p.GetType().GetProperty("Owner") ?? p.GetType().GetProperty("ContentPack");
                                    var owner = ownerProp?.GetValue(p)?.ToString() ?? string.Empty;
                                    if (owner.Contains("yourdorkbrains.cp.wildeggsforage", StringComparison.OrdinalIgnoreCase))
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                            }

                            report.Add("cp/patches", found ? WeggVerifyStatus.Pass : WeggVerifyStatus.Warn,
                                found ? "Content Patcher reports wildeggsforage patches present." : "Content Patcher did not report wildeggsforage patches (pack may be disabled/outdated).");
                        }
                        else
                        {
                            report.Add("cp/patches", WeggVerifyStatus.Warn, "Could not query Content Patcher patches (API method not found).");
                        }
                    }
                }
                catch (Exception ex)
                {
                    report.Add("cp/patches", WeggVerifyStatus.Warn, $"Could not query Content Patcher patches (best-effort): {ex.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                report.Add("cp/applied", WeggVerifyStatus.Warn, $"Post-content validation failed (best-effort): {ex.GetType().Name}");
            }
        }

    }

    private void TryProbe<T>(WeggVerifyReport report, string id, string key) where T : class
    {
        try
        {
            var asset = _helper.GameContent.Load<T>(key);
            report.Add(id, asset is null ? WeggVerifyStatus.Fail : WeggVerifyStatus.Pass, $"{key}: {(asset is null ? "null" : "loaded")}.");
        }
        catch (Exception ex)
        {
            report.Add(id, WeggVerifyStatus.Fail, $"{key}: load failed ({ex.GetType().Name}).");
        }
    }

    private static T? SafeLoad<T>(string assetName) where T : class
    {
        try { return Game1.content.Load<T>(assetName); }
        catch { return null; }
    }

    private static string RenderText(WeggVerifyReport report)
    {
        var header = $"wegg verify: PASS={report.PassCount} WARN={report.WarnCount} FAIL={report.FailCount}";
        var lines = new List<string> { header };

        foreach (var c in report.Checks)
            lines.Add($"- {c.Status.ToString().ToUpperInvariant(),4} {c.Id}: {c.Message}");

        return string.Join(Environment.NewLine, lines);
    }

    private static string RenderJson(WeggVerifyReport report)
    {
        var payload = new
        {
            ok = report.Success,
            pass = report.PassCount,
            warn = report.WarnCount,
            fail = report.FailCount,
            checks = report.Checks.Select(c => new { id = c.Id, status = c.Status.ToString(), message = c.Message }).ToList(),
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }

    private void AddBoafPipelineChecks(WeggVerifyReport report)
    {
        var editors = BoafPipelineSnapshot.EditorAssets;
        report.Add(
            "boaf/editors",
            editors.Count > 0 ? WeggVerifyStatus.Pass : WeggVerifyStatus.Warn,
            editors.Count > 0
                ? $"BOAF editors registered ({editors.Count}): {string.Join(", ", editors)}."
                : "No BOAF editor registrations reported."
        );

        var paths = BoafPipelineSnapshot.ModAssetPaths;
        report.Add(
            "boaf/modassets",
            paths.Count > 0 ? WeggVerifyStatus.Pass : WeggVerifyStatus.Warn,
            paths.Count > 0
                ? $"BOAF mod asset paths (short list): {string.Join(", ", paths)}."
                : "No BOAF mod asset paths reported."
        );
    }

}