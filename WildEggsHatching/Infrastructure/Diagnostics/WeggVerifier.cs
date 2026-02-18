using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StardewModdingAPI;
using Mikew.BirdsOfAFeather.Framework;
using WildEggsHatching.Application.AnimalNotes;
using WildEggsHatching.Infrastructure.Config;
using WildEggsHatching.Infrastructure.Logging;
using WildEggsHatching.Infrastructure.SaveData;

namespace WildEggsHatching.Infrastructure.Diagnostics;

public sealed record WeggVerifyCheck(string Id, string Status, string Message);

public sealed record WeggVerifyReport(
    bool Success,
    string Summary,
    IReadOnlyList<WeggVerifyCheck> Checks)
{
    public int PassCount => Checks.Count(x => x.Status == "pass");
    public int WarnCount => Checks.Count(x => x.Status == "warn");
    public int FailCount => Checks.Count(x => x.Status == "fail");
}

/// <summary>Runtime diagnostics command backing `wegg verify`.</summary>
public static class WeggVerifier
{
    public static WeggVerifyReport Run(
        IModHelper helper,
        ISaveDataStore store,
        IConfigProvider config,
        ILog log,
        bool full)
    {
        var checks = new List<WeggVerifyCheck>();

        // 1) BOAF presence (mandatory)
        var boafLoaded = helper.ModRegistry.IsLoaded("Mikew.BirdsOfAFeather");
        checks.Add(boafLoaded
            ? new WeggVerifyCheck("boaf.present", "pass", "BirdsOfAFeather present.")
            : new WeggVerifyCheck("boaf.present", "fail", "Missing required dependency: Mikew.BirdsOfAFeather."));


        // 1a) GMCM installed (optional)
        var gmcmLoaded = helper.ModRegistry.IsLoaded(\"spacechase0.GenericModConfigMenu\");
        checks.Add(gmcmLoaded
            ? new WeggVerifyCheck(\"gmcm.present\", \"pass\", \"Generic Mod Config Menu detected.\")
            : new WeggVerifyCheck(\"gmcm.present\", \"warn\", \"Generic Mod Config Menu not installed; config UI will be unavailable.\"));

        // 1b) BOAF API version
        try
        {
            var api = helper.ModRegistry.GetApi<IBirdsOfAFeatherApi>("Mikew.BirdsOfAFeather");
            checks.Add(api is null
                ? new WeggVerifyCheck("boaf.apiVersion", "fail", "BirdsOfAFeather API not exposed.")
                : new WeggVerifyCheck("boaf.apiVersion", "pass", $"BirdsOfAFeather API version {api.ApiVersion}."));
        }
        catch (Exception ex)
        {
            checks.Add(new WeggVerifyCheck("boaf.apiVersion", "warn", $"Failed to query BOAF API: {ex.Message}"));
        }

        // 1c) GMCM registration status (if GMCM installed)
        if (gmcmLoaded)
        {
            checks.Add(WeggDiagnosticsState.GmcmRegistered
                ? new WeggVerifyCheck("gmcm.registered", "pass", "GMCM menu registered.")
                : new WeggVerifyCheck("gmcm.registered", "warn", "GMCM installed but menu not registered (see log)."));
        }

        // 2) Local BOAF duplicates (should not exist)
        try
        {
            var localDll = Path.Combine(helper.DirectoryPath, "BirdsOfAFeather.dll");
            var localFolder = Path.Combine(helper.DirectoryPath, "BirdsOfAFeather");
            if (File.Exists(localDll) || Directory.Exists(localFolder))
                checks.Add(new WeggVerifyCheck("boaf.duplicate", "fail", "Local BirdsOfAFeather files detected inside WildEggsHatching folder."));
            else
                checks.Add(new WeggVerifyCheck("boaf.duplicate", "pass", "No local BirdsOfAFeather duplicates detected."));
        }
        catch (Exception ex)
        {
            checks.Add(new WeggVerifyCheck("boaf.duplicate", "warn", $"Duplicate scan failed: {ex.Message}"));
        }

        // 4) Asset structure + TMX image refs + schema + config sanity (existing Phase 8 checks)
        var issues = Phase8HealthReporter.CollectPhase8Issues(helper, store, config, log).ToList();
        foreach (var issue in issues)
            checks.Add(new WeggVerifyCheck("phase8.issue", issue.StartsWith("schema:", StringComparison.OrdinalIgnoreCase) ? "warn" : "warn", issue));

        // 5) AnimalNotes rules validation (requires world-ready)
        try
        {
            if (!full)
            {
                checks.Add(new WeggVerifyCheck("animalnotes.validate", "warn", "Skipped AnimalNotes validation (use --full)."));
            }
            else if (!StardewValley.Context.IsWorldReady)
            {
                checks.Add(new WeggVerifyCheck("animalnotes.validate", "warn", "World not ready; AnimalNotes validation requires a loaded save."));
            }
            else
            {
                var snapshot = new WildEggsHatching.Infrastructure.ModData.ModDataLoader(
                        new WildEggsHatching.Infrastructure.ModData.SmapiJsonFileReader(helper),
                        log).Load();

                var expectedTokens = AnimalNotesValidationRules.GetExpectedAnimalTokens(
                    wildAnimalKeys: snapshot.WildAnimals.Keys,
                    farmAnimalKeys: StardewValley.Game1.farmAnimalData.Keys);

                var objects = SafeLoad<Dictionary<string, StardewValley.GameData.Objects.ObjectData>>(WildEggsHatching.Infrastructure.Content.GameAssets.Objects) ?? new Dictionary<string, StardewValley.GameData.Objects.ObjectData>();
                var quests = SafeLoad<Dictionary<string, string>>(WildEggsHatching.Infrastructure.Content.GameAssets.Quests) ?? new Dictionary<string, string>();
                var mail = SafeLoad<Dictionary<string, string>>(WildEggsHatching.Infrastructure.Content.GameAssets.Mail) ?? new Dictionary<string, string>();

                var result = AnimalNotesValidationRules.Analyze(
                    expectedTokens: expectedTokens,
                    presentNoteIds: objects.Keys,
                    presentQuestIds: quests.Keys,
                    presentMailIds: mail.Keys);

                var missing = result.MissingNotes.Count + result.MissingQuests.Count + result.MissingMail.Count;
                var extra = result.ExtraNotes.Count + result.ExtraQuests.Count + result.ExtraMail.Count;

                if (missing == 0 && extra == 0)
                    checks.Add(new WeggVerifyCheck("animalnotes.validate", "pass", $"AnimalNotes validation ok (animals={expectedTokens.Count})."));
                else
                    checks.Add(new WeggVerifyCheck("animalnotes.validate", "warn", $"AnimalNotes validation: animals={expectedTokens.Count} missing={missing} extra={extra}."));
            }
        }
        catch (Exception ex)
        {
            checks.Add(new WeggVerifyCheck("animalnotes.validate", "warn", $"AnimalNotes validation failed: {ex.Message}"));
        }
var fail = checks.Any(c => c.Status == "fail");
        var summary = $"PASS: {checks.Count(c => c.Status == "pass")}  WARN: {checks.Count(c => c.Status == "warn")}  FAIL: {checks.Count(c => c.Status == "fail")}";
        return new WeggVerifyReport(!fail, summary, checks);

    }

    private static T? SafeLoad<T>(string assetName) where T : class
    {
        try { return StardewValley.Game1.content.Load<T>(assetName); }
        catch { return null; }
    }
}
