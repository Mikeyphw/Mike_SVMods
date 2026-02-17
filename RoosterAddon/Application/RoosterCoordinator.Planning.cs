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

    private readonly DailyHatchingPlanService _planService = new();
    private readonly DailyHatchingPlanApplier _planApplier = new();

    private DailyHatchingPlanEnvelope? _dailyPlan;
    private int _dailyPlanDay = -1;
    private string _dailyPlanFingerprint = string.Empty;

    private readonly Dictionary<string, List<string>> _otherAnimalsCache =
        new(StringComparer.OrdinalIgnoreCase);
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
        var farmId = Game1.uniqueIDForThisGame;

        // Fingerprint depends on daily luck + roosters/females.
        var fingerprint = _planService.ComputeFingerprint(
            config: Config,
            roosterHelper: RoosterHelper,
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

        DailyHatchingPlanEnvelope? loaded = null;
        try
        {
            loaded = _saveData.Read<DailyHatchingPlanEnvelope>(DailyPlanSaveKey);
        }
        catch
        {
            loaded = null;
        }

        if (!forceRecompute
            && loaded is not null
            && loaded.Day == today
            && loaded.FarmId == farmId
            && string.Equals(loaded.Fingerprint, fingerprint, StringComparison.OrdinalIgnoreCase))
        {
            _dailyPlan = loaded;
            _dailyPlanDay = today;
            _dailyPlanFingerprint = fingerprint;
            usedHostPlan = Context.IsMainPlayer;
            return _dailyPlan;
        }

        // If not host, try to use what we loaded even if fingerprint mismatched (best effort).
        if (!Context.IsMainPlayer && loaded is not null && loaded.Day == today && loaded.FarmId == farmId)
        {
            _dailyPlan = loaded;
            _dailyPlanDay = today;
            _dailyPlanFingerprint = loaded.Fingerprint;
            usedHostPlan = false;
            return _dailyPlan;
        }

        // Compute a new plan (host authoritative) or ephemeral fallback for clients.
        var nonce = forceRecompute
            ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            : (loaded?.Nonce ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        var built = _planService.BuildPlan(
            farmAnimalsData: farmAnimalsData,
            config: Config,
            roosterHelper: RoosterHelper,
            dailyLuck: DailyLuck,
            farmId: farmId,
            day: today,
            nonce: nonce
        );

        _dailyPlan = built;
        _dailyPlanDay = today;
        _dailyPlanFingerprint = fingerprint;

        if (Context.IsMainPlayer)
        {
            usedHostPlan = true;
            _saveData.Write(DailyPlanSaveKey, built);
        }

        return built;
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
            try
            {
                var data = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
                _ = EnsureDailyPlanForToday(data, forceRecompute: false, out _);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to load {FarmAnimalsAssetName} for plan dump: {ex}", LogLevel.Error);
                return;
            }

            var now = DateTimeOffset.Now;
            var fileName = Infrastructure.StateDumps.StateDumpJson.BuildFileName("roosteraddon_daily_plan", now);
            WriteStateDump(fileName, _dailyPlan ?? new { error = "plan not available" });
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

            // Force a re-apply this tick.
            RequestInvalidate(FarmAnimalsAssetName);
            RequestInvalidate(ObjectsAssetName);

            Monitor.Log("Daily plan recomputed and assets invalidated.", LogLevel.Info);
            return;
        }

        Monitor.Log($"Unknown subcommand '{verb}'. Run: ra_plan help", LogLevel.Warn);
    }
}
