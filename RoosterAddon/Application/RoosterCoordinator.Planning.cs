using System;
using System.Collections.Generic;
using System.Linq;
using RoosterAddon.Application.Planning;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.FarmAnimals;

namespace RoosterAddon.Application;

internal sealed partial class RoosterCoordinator
{
    private string DailyPlanSaveKey => $"{ModManifest.UniqueID}/daily-hatching-plan-v1";
    private string DailyPlanBackupSaveKey => $"{ModManifest.UniqueID}/daily-hatching-plan-v1.backup";

    private readonly DailyHatchingPlanService _planService = new();
    private readonly DailyHatchingPlanApplier _planApplier = new();

    private DailyHatchingPlanEnvelope? _dailyPlan;
    private int _dailyPlanDay = -1;
    private string _dailyPlanFingerprint = string.Empty;

    private readonly Dictionary<string, List<string>> _otherAnimalsCache = new(StringComparer.OrdinalIgnoreCase);
    private int _otherAnimalsCacheDay = -1;
    private long _otherAnimalsCacheNonce;

    private void InvalidateRuleCaches(string reason)
    {
        _otherAnimalsCache.Clear();
        _otherAnimalsCacheDay = -1;
        _otherAnimalsCacheNonce = 0;
    }

    private void InvalidateDailyPlanCache(string reason)
    {
        _dailyPlan = null;
        _dailyPlanDay = -1;
        _dailyPlanFingerprint = string.Empty;
        InvalidateRuleCaches(reason);
    }

    private DailyHatchingPlanEnvelope? EnsureDailyPlanForToday(
        IDictionary<string, FarmAnimalData> farmAnimalsData,
        bool forceRecompute,
        out bool usedHostPlan
    )
    {
        usedHostPlan = false;

        if (!Context.IsWorldReady)
            return null;

        var today = StardewModdingAPI.Utilities.SDate.Now().DaysSinceStart;
        var farmId = unchecked((long)Game1.uniqueIDForThisGame);

        var fingerprint = _planService.ComputeFingerprint(
            config: Config,
            roosters: RoosterHelper.Roosters,
            femaleEggs: RoosterHelper.FemaleEggs,
            dailyLuck: DailyLuck,
            farmId: farmId,
            day: today
        );

        if (!forceRecompute
            && _dailyPlan is not null
            && _dailyPlanDay == today
            && string.Equals(_dailyPlanFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase))
        {
            usedHostPlan = Context.IsMainPlayer;
            return _dailyPlan;
        }

        var loaded = TryLoadSavedPlan();

        if (!forceRecompute && loaded is not null && loaded.Day == today && loaded.FarmId == farmId)
        {
            var fingerprintMatches = string.Equals(loaded.Fingerprint, fingerprint, StringComparison.OrdinalIgnoreCase);
            var requireCompleteness = fingerprintMatches;

            if (!TryValidateDailyPlan(
                    plan: loaded,
                    farmAnimalsData: farmAnimalsData,
                    expectedFingerprint: fingerprint,
                    out var invalidReason,
                    requireCompleteness: requireCompleteness))
            {
                Monitor.Log($"Daily plan invalid/corrupt. {invalidReason}", LogLevel.Warn);

                if (Context.IsMainPlayer)
                    BackupAndDeleteCorruptPlan(loaded, invalidReason);

                loaded = null;
            }
            else
            {
                if (fingerprintMatches)
                {
                    CacheDailyPlan(loaded, today, fingerprint);
                    usedHostPlan = Context.IsMainPlayer;
                    return _dailyPlan;
                }

                if (!Context.IsMainPlayer)
                {
                    CacheDailyPlan(loaded, today, loaded.Fingerprint);
                    usedHostPlan = false;
                    return _dailyPlan;
                }
            }
        }

        return BuildAndMaybePersistPlan(farmAnimalsData, farmId, today, fingerprint, forceRecompute, out usedHostPlan);
    }

    private DailyHatchingPlanEnvelope? TryLoadSavedPlan()
    {
        try
        {
            return _saveData.Read<DailyHatchingPlanEnvelope>(DailyPlanSaveKey);
        }
        catch
        {
            return null;
        }
    }

    private void CacheDailyPlan(DailyHatchingPlanEnvelope plan, int day, string fingerprint)
    {
        _dailyPlan = plan;
        _dailyPlanDay = day;
        _dailyPlanFingerprint = fingerprint;
    }

    private DailyHatchingPlanEnvelope BuildAndMaybePersistPlan(
        IDictionary<string, FarmAnimalData> farmAnimalsData,
        long farmId,
        int day,
        string fingerprint,
        bool forceRecompute,
        out bool usedHostPlan
    )
    {
        var nonce = forceRecompute
            ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var built = _planService.BuildPlan(
            farmAnimalsData: farmAnimalsData,
            config: Config,
            roosters: RoosterHelper.Roosters,
            femaleEggs: RoosterHelper.FemaleEggs,
            dailyLuck: DailyLuck,
            farmId: farmId,
            day: day,
            nonce: nonce
        );

        CacheDailyPlan(built, day, fingerprint);

        if (Context.IsMainPlayer)
        {
            usedHostPlan = true;
            _saveData.Write(DailyPlanSaveKey, built);
        }
        else
        {
            usedHostPlan = false;
        }

        return built;
    }

    private bool TryValidateDailyPlan(
        DailyHatchingPlanEnvelope plan,
        IDictionary<string, FarmAnimalData> farmAnimalsData,
        string expectedFingerprint,
        out string reason,
        bool requireCompleteness
    )
    {
        reason = string.Empty;

        if (plan is null)
        {
            reason = "Plan is null.";
            return false;
        }

        if (plan.Version != 1)
        {
            reason = $"Unsupported plan version: {plan.Version}.";
            return false;
        }

        if (plan.FarmId <= 0)
        {
            reason = "FarmId missing/invalid.";
            return false;
        }

        if (plan.Day < 0)
        {
            reason = "Day missing/invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(plan.Fingerprint))
        {
            reason = "Fingerprint missing.";
            return false;
        }

        if (plan.Buildings is null)
        {
            reason = "Buildings map is null.";
            return false;
        }

        foreach (var (bKey, building) in plan.Buildings)
        {
            if (building is null)
            {
                reason = $"Building '{bKey}' is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(building.BuildingKey))
            {
                reason = $"Building '{bKey}' has empty BuildingKey.";
                return false;
            }

            if (building.Roosters is null)
            {
                reason = $"Building '{building.BuildingKey}' has null roosters map.";
                return false;
            }

            foreach (var (rKey, roosterPlan) in building.Roosters)
            {
                if (roosterPlan is null)
                {
                    reason = $"Rooster plan '{rKey}' in '{building.BuildingKey}' is null.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(roosterPlan.RoosterKey))
                {
                    reason = $"Rooster plan '{rKey}' in '{building.BuildingKey}' has empty RoosterKey.";
                    return false;
                }

                if (roosterPlan.Females is null)
                {
                    reason = $"Rooster '{roosterPlan.RoosterKey}' in '{building.BuildingKey}' has null females map.";
                    return false;
                }

                foreach (var (fKey, femalePlan) in roosterPlan.Females)
                {
                    if (femalePlan is null)
                    {
                        reason = $"Female plan '{fKey}' in rooster '{roosterPlan.RoosterKey}' is null.";
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(femalePlan.FemaleKey))
                    {
                        reason = $"Female plan '{fKey}' in rooster '{roosterPlan.RoosterKey}' has empty FemaleKey.";
                        return false;
                    }

                    if (femalePlan.NormalEgg is not null
                        && (femalePlan.NormalEgg.ChancePercent < 0 || femalePlan.NormalEgg.ChancePercent > 100))
                    {
                        reason = $"NormalEgg chance out of range for '{femalePlan.FemaleKey}'.";
                        return false;
                    }

                    if (femalePlan.LargeEgg is not null
                        && (femalePlan.LargeEgg.ChancePercent < 0 || femalePlan.LargeEgg.ChancePercent > 100))
                    {
                        reason = $"LargeEgg chance out of range for '{femalePlan.FemaleKey}'.";
                        return false;
                    }

                    if (femalePlan.FertilizedOnly is not null)
                    {
                        var fo = femalePlan.FertilizedOnly;

                        if (fo.FemaleUpgradeChancePercent < 0f || fo.FemaleUpgradeChancePercent > 100f)
                        {
                            reason = $"Fertilized upgrade chance out of range for '{femalePlan.FemaleKey}'.";
                            return false;
                        }

                        if (fo.RoosterToChickenRatioPercent < 0 || fo.RoosterToChickenRatioPercent > 100)
                        {
                            reason = $"Rooster ratio out of range for '{femalePlan.FemaleKey}'.";
                            return false;
                        }

                        if (string.IsNullOrWhiteSpace(fo.RoosterFertilizedEggId) || string.IsNullOrWhiteSpace(fo.FemaleFertilizedEggId))
                        {
                            reason = $"Fertilized egg ids missing for '{femalePlan.FemaleKey}'.";
                            return false;
                        }
                    }
                }
            }
        }

        if (requireCompleteness && string.Equals(plan.Fingerprint, expectedFingerprint, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var rooster in RoosterHelper.Roosters.Values)
            {
                var buildingKey = ResolveBuildingKey(rooster, farmAnimalsData);

                if (!plan.Buildings.TryGetValue(buildingKey, out var building))
                {
                    reason = $"Missing building '{buildingKey}' for rooster '{rooster.FarmAnimalName}'.";
                    return false;
                }

                if (!building.Roosters.TryGetValue(rooster.FarmAnimalName, out var roosterPlan))
                {
                    reason = $"Missing rooster '{rooster.FarmAnimalName}' in building '{buildingKey}'.";
                    return false;
                }

                foreach (var femaleKey in rooster.Females)
                {
                    if (!roosterPlan.Females.TryGetValue(femaleKey, out _))
                    {
                        reason = $"Missing female '{femaleKey}' under rooster '{rooster.FarmAnimalName}'.";
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private void BackupAndDeleteCorruptPlan(DailyHatchingPlanEnvelope plan, string reason)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        try
        {
            var backup = new DailyHatchingPlanCorruptBackup
            {
                TimestampUtcUnixMs = now,
                Reason = reason,
                Plan = plan
            };

            _saveData.Write(DailyPlanBackupSaveKey, backup);
        }
        catch (Exception ex)
        {
            Monitor.Log($"Failed to write daily plan backup: {ex}", LogLevel.Warn);
        }

        try
        {
            var fileName = Infrastructure.StateDumps.StateDumpJson.BuildFileName("roosteraddon_daily_plan_corrupt", DateTimeOffset.UtcNow);
            WriteStateDump(fileName, new { reason, plan });
            Monitor.Log($"Wrote corrupt plan dump: {fileName}", LogLevel.Info);
        }
        catch
        {
            // best effort
        }

        try
        {
            _saveData.Delete(DailyPlanSaveKey);
        }
        catch (Exception ex)
        {
            Monitor.Log($"Failed to delete corrupt daily plan save data: {ex}", LogLevel.Warn);
        }
    }

    private IReadOnlyList<string> GetOtherAnimalsSharingEggsCached(
        IDictionary<string, FarmAnimalData> farmAnimalsData,
        RoosterData rooster,
        DailyHatchingPlanEnvelope? plan
    )
    {
        var today = StardewModdingAPI.Utilities.SDate.Now().DaysSinceStart;
        var nonce = plan?.Nonce ?? 0;

        if (_otherAnimalsCacheDay != today || _otherAnimalsCacheNonce != nonce)
        {
            _otherAnimalsCacheDay = today;
            _otherAnimalsCacheNonce = nonce;
            _otherAnimalsCache.Clear();
        }

        if (_otherAnimalsCache.TryGetValue(rooster.FarmAnimalName, out var cached))
            return cached;

        var computed = DailyHatchingPlanApplier.GetAnimalsThatProduceRoosterEgg(farmAnimalsData, rooster);
        _otherAnimalsCache[rooster.FarmAnimalName] = computed;
        return computed;
    }

    private static string ResolveBuildingKey(RoosterData rooster, IDictionary<string, FarmAnimalData> data)
    {
        if (data.TryGetValue(rooster.FarmAnimalName, out var entry) && !string.IsNullOrWhiteSpace(entry.RequiredBuilding))
            return entry.RequiredBuilding.Trim();

        return rooster.BuildingRequired switch
        {
            Framework.Config.RequiredBuilding.Coop => "Coop",
            Framework.Config.RequiredBuilding.BigCoop => "Big Coop",
            Framework.Config.RequiredBuilding.DeluxeCoop => "Deluxe Coop",
            _ => "Big Coop",
        };
    }

    private void DailyPlanCommand(string command, string[] args)
    {
        var verb = args.FirstOrDefault()?.Trim() ?? "help";

        if (verb.Equals("help", StringComparison.OrdinalIgnoreCase) || verb.Equals("-h", StringComparison.OrdinalIgnoreCase))
        {
            Monitor.Log(
                "ra_plan usage:\n" +
                "  ra_plan dump\n" +
                "    - writes the current daily plan JSON to data/state-dumps (computes if missing).\n" +
                "  ra_plan recompute\n" +
                "    - DEV ONLY (EnableDebug=true). Host recomputes plan (new nonce) and invalidates assets.",
                LogLevel.Info
            );
            return;
        }

        if (!Context.IsWorldReady)
        {
            Monitor.Log("Save not loaded; daily plan requires a loaded save.", LogLevel.Info);
            return;
        }

        if (verb.Equals("dump", StringComparison.OrdinalIgnoreCase))
        {
            Dictionary<string, FarmAnimalData> data;
            try
            {
                data = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to load {FarmAnimalsAssetName} for plan dump: {ex}", LogLevel.Error);
                return;
            }

            _ = EnsureDailyPlanForToday(data, forceRecompute: false, out _);

            var fileName = Infrastructure.StateDumps.StateDumpJson.BuildFileName("roosteraddon_daily_plan", DateTimeOffset.Now);
            if (_dailyPlan is not null)
                WriteStateDump(fileName, _dailyPlan);
            else
                WriteStateDump(fileName, new { error = "plan not available" });
            Monitor.Log($"Wrote plan dump: {fileName}", LogLevel.Info);
            return;
        }

        if (verb.Equals("recompute", StringComparison.OrdinalIgnoreCase))
        {
            if (!Config.General.EnableDebug)
            {
                Monitor.Log("EnableDebug is false; recompute is dev-only.", LogLevel.Warn);
                return;
            }

            if (!Context.IsMainPlayer)
            {
                Monitor.Log("Only the host can recompute the plan.", LogLevel.Warn);
                return;
            }

            Dictionary<string, FarmAnimalData> data;
            try
            {
                data = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to load {FarmAnimalsAssetName} for recompute: {ex}", LogLevel.Error);
                return;
            }

            _ = EnsureDailyPlanForToday(data, forceRecompute: true, out _);

            RequestInvalidate(FarmAnimalsAssetName);
            RequestInvalidate(ObjectsAssetName);

            Monitor.Log("Daily plan recomputed and assets invalidated.", LogLevel.Info);
            return;
        }

        Monitor.Log($"Unknown subcommand '{verb}'. Run: ra_plan help", LogLevel.Warn);
    }
}
