#if RCTRL_DEV
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using RoosterControl.Application.Planning;
using RoosterControl.Helpers;
using RoosterControl.Enums;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.FarmAnimals;

namespace RoosterControl.Application;

internal sealed partial class RoosterCoordinator
{
    private static (List<string> Positional, bool Full, bool Json) ParseDebugArgs(string[] args)
    {
        var positional = new List<string>();
        var full = false;
        var json = false;

        foreach (var raw in args ?? Array.Empty<string>())
        {
            var token = (raw ?? string.Empty).Trim();
            if (token.Length == 0)
                continue;

            if (token.Equals("--full", StringComparison.OrdinalIgnoreCase))
            {
                full = true;
                continue;
            }

            if (token.Equals("--json", StringComparison.OrdinalIgnoreCase))
            {
                json = true;
                continue;
            }

            positional.Add(token);
        }

        return (positional, full, json);
    }

    private static string NormalizeBuildingKey(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var t = raw.Trim();

        // Common user inputs.
        if (t.Equals("DeluxeCoop", StringComparison.OrdinalIgnoreCase) || t.Equals("Deluxe_Coop", StringComparison.OrdinalIgnoreCase))
            return "Deluxe Coop";
        if (t.Equals("BigCoop", StringComparison.OrdinalIgnoreCase) || t.Equals("Big_Coop", StringComparison.OrdinalIgnoreCase))
            return "Big Coop";
        if (t.Equals("Coop", StringComparison.OrdinalIgnoreCase))
            return "Coop";

        // Normalize underscores/hyphens to spaces.
        t = t.Replace('_', ' ').Replace('-', ' ');

        return t;
    }

    private void DebugProduceCommand(string _command, string[] args)
    {
        var (positional, full, json) = ParseDebugArgs(args);

        if (positional.Count == 0 || positional[0].Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            Monitor.Log(
                "Usage: ra_debug_produce [--full] [--json] <roosterType> [day] [locationName] [buildingType]\n" +
                "Examples:\n" +
                "  ra_debug_produce WhiteRooster\n" +
                "  ra_debug_produce WhiteRooster --full\n" +
                "  ra_debug_produce WhiteRooster --json\n" +
                "  ra_debug_produce WhiteRooster 1234 Farm DeluxeCoop --full",
                LogLevel.Info
            );
            return;
        }

        if (RoosterHelper is null)
        {
            Monitor.Log("[ra_debug_produce] RoosterHelper is not initialized yet.", LogLevel.Warn);
            return;
        }

        if (!Enum.TryParse<RoosterType>(positional[0], ignoreCase: true, out var roosterType))
        {
            Monitor.Log($"[ra_debug_produce] Unknown roosterType '{positional[0]}'. Valid: {string.Join(", ", Enum.GetNames(typeof(RoosterType)))}", LogLevel.Warn);
            return;
        }

        int? dayOverride = null;
        if (positional.Count >= 2 && int.TryParse(positional[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var day))
            dayOverride = day;

        var locationOverride = positional.Count >= 3 ? positional[2] : null;
        var buildingOverride = positional.Count >= 4 ? positional[3] : null;

        var dump = RoosterHelper.DebugDumpChosenProduce(
            roosterType: roosterType,
            dayOverride: dayOverride,
            locationNameOverride: locationOverride,
            buildingTypeOverride: buildingOverride,
            full: full,
            json: json
        );

        Monitor.Log(dump, LogLevel.Info);
    }

    private void DebugPlanCommand(string _command, string[] args)
    {
        var (positional, full, json) = ParseDebugArgs(args);

        if (positional.Count > 0 && positional[0].Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            Monitor.Log(
                "Usage: ra_debug_plan [--full] [--json] [day] [locationName] [buildingType]\n" +
                "Examples:\n" +
                "  ra_debug_plan\n" +
                "  ra_debug_plan 1234 Farm DeluxeCoop\n" +
                "  ra_debug_plan --json",
                LogLevel.Info
            );
            return;
        }

        if (!Context.IsWorldReady)
        {
            Monitor.Log("[ra_debug_plan] Save not loaded.", LogLevel.Info);
            return;
        }

        if (RoosterHelper is null)
        {
            Monitor.Log("[ra_debug_plan] RoosterHelper is not initialized yet.", LogLevel.Warn);
            return;
        }

        int? dayOverride = null;
        string? locationOverride = null;
        string? buildingOverride = null;

        if (positional.Count >= 1 && int.TryParse(positional[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var day))
        {
            dayOverride = day;

            if (positional.Count == 2)
                buildingOverride = positional[1];
            else
            {
                if (positional.Count >= 2)
                    locationOverride = positional[1];
                if (positional.Count >= 3)
                    buildingOverride = positional[2];
            }
        }
        else if (positional.Count >= 1)
        {
            buildingOverride = positional[0];
        }

        var buildingKeyFilter = string.IsNullOrWhiteSpace(buildingOverride)
            ? null
            : NormalizeBuildingKey(buildingOverride);

        Dictionary<string, FarmAnimalData> farmAnimalsData;
        try
        {
            farmAnimalsData = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
        }
        catch (Exception ex)
        {
            Monitor.Log($"[ra_debug_plan] Failed to load {FarmAnimalsAssetName}: {ex}", LogLevel.Error);
            return;
        }

        var today = SDate.Now().DaysSinceStart;
        var farmId = unchecked((long)Game1.uniqueIDForThisGame);

        DailyHatchingPlanEnvelope? plan = null;
        var usedAdHoc = false;

        if (dayOverride is null || dayOverride.Value == today)
        {
            plan = _dailyDecisions?.Plan ?? _dailyPlan ?? EnsureDailyPlanForToday(farmAnimalsData, forceRecompute: false, out _);
        }
        else
        {
            // Deterministic debug plan for an arbitrary day (not persisted).
            usedAdHoc = true;
            plan = _planService.BuildPlan(
                farmAnimalsData: farmAnimalsData,
                config: Config,
                roosters: RoosterHelper.Roosters,
                femaleEggs: RoosterHelper.FemaleEggs,
                dailyLuck: DailyLuck,
                farmId: farmId,
                day: dayOverride.Value,
                nonce: 0
            );
        }

        if (plan is null)
        {
            Monitor.Log("[ra_debug_plan] No plan available.", LogLevel.Info);
            return;
        }

        if (json)
        {
            var dump = BuildDebugPlanDump(plan, farmAnimalsData, buildingKeyFilter, full, usedAdHoc, locationOverride);
            Monitor.Log(JsonSerializer.Serialize(dump, new JsonSerializerOptions { WriteIndented = false }), LogLevel.Info);
            return;
        }

        Monitor.Log(BuildDebugPlanText(plan, farmAnimalsData, buildingKeyFilter, full, usedAdHoc, locationOverride), LogLevel.Info);
    }

    private void DebugDiffCommand(string _command, string[] args)
    {
        var (positional, full, json) = ParseDebugArgs(args);

        if (positional.Count > 0 && positional[0].Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            Monitor.Log(
                "Usage: ra_debug_diff [--full] [--json]\n" +
                "Examples:\n" +
                "  ra_debug_diff\n" +
                "  ra_debug_diff --full\n" +
                "  ra_debug_diff --json",
                LogLevel.Info
            );
            return;
        }

        if (_dailyDecisions is null)
        {
            Monitor.Log("[ra_debug_diff] No current daily decisions snapshot.", LogLevel.Info);
            return;
        }

        if (_previousDailyDecisions is null)
        {
            Monitor.Log("[ra_debug_diff] No previous daily decisions snapshot (nothing to diff yet).", LogLevel.Info);
            return;
        }

        Dictionary<string, FarmAnimalData>? farmAnimalsData = null;
        if (Context.IsWorldReady)
        {
            try
            {
                farmAnimalsData = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
            }
            catch
            {
                farmAnimalsData = null;
            }
        }

        var prevDump = BuildDebugDecisionsDump(_previousDailyDecisions, farmAnimalsData, full);
        var curDump = BuildDebugDecisionsDump(_dailyDecisions, farmAnimalsData, full);

        var diff = BuildDebugDiffDump(prevDump, curDump);

        if (json)
        {
            Monitor.Log(JsonSerializer.Serialize(diff, new JsonSerializerOptions { WriteIndented = false }), LogLevel.Info);
            return;
        }

        Monitor.Log(BuildDebugDiffText(diff), LogLevel.Info);
    }

    private void DebugPipelineCommand(string _command, string[] args)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"WorldReady={Context.IsWorldReady} MainPlayer={Context.IsMainPlayer} HasDayStarted={HasDayStarted}");
        sb.AppendLine($"FarmAnimalsRefreshQueued={_farmAnimalsRefreshQueued}");

        if (_debugInvalidationsRequested.Count > 0)
            sb.AppendLine($"InvalidationsRequested={string.Join(", ", _debugInvalidationsRequested)}");
        else
            sb.AppendLine("InvalidationsRequested=<none>");

        if (!Context.IsWorldReady)
        {
            Monitor.Log(sb.ToString().TrimEnd(), LogLevel.Info);
            return;
        }

        var today = SDate.Now().DaysSinceStart;
        var farmId = unchecked((long)Game1.uniqueIDForThisGame);

        sb.AppendLine($"Day={today} FarmId={farmId} DailyLuck={DailyLuck}");
        sb.AppendLine($"DailyPlanFingerprint={_dailyPlanFingerprint}");

        if (_dailyDecisions is null)
        {
            sb.AppendLine("DailyDecisions=<null>");
            Monitor.Log(sb.ToString().TrimEnd(), LogLevel.Info);
            return;
        }

        sb.AppendLine($"DailyDecisions.Day={_dailyDecisions.Day} FarmId={_dailyDecisions.FarmId} Fingerprint={_dailyDecisions.Fingerprint}");
        sb.AppendLine($"DailyDecisions.MatchesToday={_dailyDecisions.MatchesToday(farmId, today)}");

        var plan = _dailyDecisions.Plan;
        if (plan is null)
        {
            sb.AppendLine("DailyPlan=<null>");
            Monitor.Log(sb.ToString().TrimEnd(), LogLevel.Info);
            return;
        }

        var planBuildings = plan.Buildings;
        var buildings = planBuildings?.Count ?? 0;
        var roosterCount = 0;
        var femaleCount = 0;
        var normalEggPlans = 0;
        var largeEggPlans = 0;
        var fertilizedOnlyPlans = 0;

        if (planBuildings is null)
        {
            sb.AppendLine($"DailyPlan: version={plan.Version} nonce={plan.Nonce} fingerprint={plan.Fingerprint}");
            sb.AppendLine("DailyPlanCounts: buildings=0 roosters=0 females=0 eggPlans(normal=0, large=0) fertilizedOnly=0");
            Monitor.Log(sb.ToString().TrimEnd(), LogLevel.Info);
            return;
        }

        foreach (var b in planBuildings.Values)
        {
            roosterCount += b.Roosters.Count;
            foreach (var r in b.Roosters.Values)
            {
                femaleCount += r.Females.Count;
                foreach (var f in r.Females.Values)
                {
                    if (f.NormalEgg is not null) normalEggPlans++;
                    if (f.LargeEgg is not null) largeEggPlans++;
                    if (f.FertilizedOnly is not null) fertilizedOnlyPlans++;
                }
            }
        }

        sb.AppendLine($"DailyPlan: version={plan.Version} nonce={plan.Nonce} fingerprint={plan.Fingerprint}");
        sb.AppendLine($"DailyPlanCounts: buildings={buildings} roosters={roosterCount} females={femaleCount} eggPlans(normal={normalEggPlans}, large={largeEggPlans}) fertilizedOnly={fertilizedOnlyPlans}");

        Monitor.Log(sb.ToString().TrimEnd(), LogLevel.Info);
    }

    private sealed record DebugDecisionsDump(
        int Day,
        long FarmId,
        string Fingerprint,
        DebugPlanDump? Plan
    );

    private sealed record DebugPlanDump(
        int Day,
        long FarmId,
        string Fingerprint,
        long Nonce,
        bool AdHoc,
        string? LocationName,
        List<DebugBuildingPlan> Buildings
    );

    private sealed record DebugBuildingPlan(string BuildingKey, List<DebugRoosterPlan> Roosters);
    private sealed record DebugRoosterPlan(string RoosterKey, List<DebugFemalePlan> Females);

    private sealed record DebugFemalePlan(
        string FemaleKey,
        DebugEggDecision? NormalEgg,
        DebugEggDecision? LargeEgg,
        DebugFertilizedDecision? FertilizedOnly
    );

    private sealed record DebugEggDecision(
        bool Enabled,
        int ChancePercent,
        bool RolledSuccess,
        string EggIdToAddToRooster,
        string? FemaleEggIdToRemove,
        string Decision,
        List<string>? RemovedFromOtherAnimals
    );

    private sealed record DebugFertilizedDecision(
        float FemaleUpgradeChancePercent,
        int RoosterToChickenRatioPercent,
        bool UpgradeFemaleProduceToFertilized,
        bool RoosterHatchesFertilizedEgg,
        string RoosterFertilizedEggId,
        string FemaleFertilizedEggId,
        string Decision
    );

    private DebugDecisionsDump BuildDebugDecisionsDump(
        DailyFarmAnimalsDecisions decisions,
        IDictionary<string, FarmAnimalData>? farmAnimalsData,
        bool full
    )
    {
        return new DebugDecisionsDump(
            Day: decisions.Day,
            FarmId: decisions.FarmId,
            Fingerprint: decisions.Fingerprint,
            Plan: decisions.Plan is null
                ? null
                : BuildDebugPlanDump(
                    plan: decisions.Plan,
                    farmAnimalsData: farmAnimalsData,
                    buildingKeyFilter: null,
                    full: full,
                    adHoc: false,
                    locationName: null
                )
        );
    }

    private DebugPlanDump BuildDebugPlanDump(
        DailyHatchingPlanEnvelope plan,
        IDictionary<string, FarmAnimalData>? farmAnimalsData,
        string? buildingKeyFilter,
        bool full,
        bool adHoc,
        string? locationName
    )
    {
        var buildings = new List<DebugBuildingPlan>();

        foreach (var (buildingKey, building) in plan.Buildings.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(buildingKeyFilter)
                && !buildingKey.Contains(buildingKeyFilter.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            var roosters = new List<DebugRoosterPlan>();

            foreach (var (roosterKey, roosterPlan) in building.Roosters.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                var females = new List<DebugFemalePlan>();

                var otherAnimalsSharingEggs = new List<string>();

                if (farmAnimalsData is not null)
                {
                    var roosterData = FindRoosterDataByFarmAnimalKey(roosterKey);
                    if (roosterData is not null)
                        otherAnimalsSharingEggs = DailyHatchingPlanApplier.GetAnimalsThatProduceRoosterEgg(farmAnimalsData, roosterData);
                }

                foreach (var (femaleKey, femalePlan) in roosterPlan.Females.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
                {
                    females.Add(new DebugFemalePlan(
                        FemaleKey: femaleKey,
                        NormalEgg: femalePlan.NormalEgg is null ? null : BuildEggDecision(femalePlan.NormalEgg, otherAnimalsSharingEggs, full),
                        LargeEgg: femalePlan.LargeEgg is null ? null : BuildEggDecision(femalePlan.LargeEgg, otherAnimalsSharingEggs, full),
                        FertilizedOnly: femalePlan.FertilizedOnly is null ? null : BuildFertilizedDecision(femalePlan.FertilizedOnly)
                    ));
                }

                roosters.Add(new DebugRoosterPlan(roosterKey, females));
            }

            buildings.Add(new DebugBuildingPlan(buildingKey, roosters));
        }

        return new DebugPlanDump(
            Day: plan.Day,
            FarmId: plan.FarmId,
            Fingerprint: plan.Fingerprint,
            Nonce: plan.Nonce,
            AdHoc: adHoc,
            LocationName: locationName,
            Buildings: buildings
        );
    }

    private RoosterData? FindRoosterDataByFarmAnimalKey(string roosterKey)
    {
        if (RoosterHelper is null)
            return null;

        foreach (var r in RoosterHelper.Roosters.Values)
        {
            if (string.Equals(r.FarmAnimalName, roosterKey, StringComparison.OrdinalIgnoreCase))
                return r;
        }

        return null;
    }

    private static DebugEggDecision BuildEggDecision(EggChancePlan plan, List<string> otherAnimalsSharingEggs, bool full)
    {
        var decision = BuildEggDecisionReason(plan);

        return new DebugEggDecision(
            Enabled: plan.Enabled,
            ChancePercent: plan.ChancePercent,
            RolledSuccess: plan.RolledSuccess,
            EggIdToAddToRooster: plan.EggIdToAddToRooster ?? string.Empty,
            FemaleEggIdToRemove: plan.FemaleEggIdToRemove,
            Decision: decision,
            RemovedFromOtherAnimals: full && plan.RolledSuccess && !string.IsNullOrWhiteSpace(plan.EggIdToAddToRooster)
                ? otherAnimalsSharingEggs.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList()
                : null
        );
    }

    private static string BuildEggDecisionReason(EggChancePlan plan)
    {
        if (plan is null)
            return "skip: plan missing";

        if (!plan.Enabled)
            return "skip: disabled";

        if (plan.ChancePercent <= 0)
            return "skip: chance 0";

        if (string.IsNullOrWhiteSpace(plan.EggIdToAddToRooster))
            return $"skip: missing egg id (chance {plan.ChancePercent}%)";

        if (!plan.RolledSuccess)
            return $"skip: roll failed ({plan.ChancePercent}%)";

        var tail = string.IsNullOrWhiteSpace(plan.FemaleEggIdToRemove)
            ? string.Empty
            : $" removeFemaleEgg={plan.FemaleEggIdToRemove}";

        return $"apply: roll success ({plan.ChancePercent}%) addEgg={plan.EggIdToAddToRooster}{tail}";
    }

    private static DebugFertilizedDecision BuildFertilizedDecision(FertilizedOnlyPlan plan)
    {
        var decision = BuildFertilizedDecisionReason(plan);

        return new DebugFertilizedDecision(
            FemaleUpgradeChancePercent: plan.FemaleUpgradeChancePercent,
            RoosterToChickenRatioPercent: plan.RoosterToChickenRatioPercent,
            UpgradeFemaleProduceToFertilized: plan.UpgradeFemaleProduceToFertilized,
            RoosterHatchesFertilizedEgg: plan.RoosterHatchesFertilizedEgg,
            RoosterFertilizedEggId: plan.RoosterFertilizedEggId ?? string.Empty,
            FemaleFertilizedEggId: plan.FemaleFertilizedEggId ?? string.Empty,
            Decision: decision
        );
    }

    private static string BuildFertilizedDecisionReason(FertilizedOnlyPlan plan)
    {
        if (plan is null)
            return "skip: plan missing";

        if (plan.UpgradeFemaleProduceToFertilized && !string.IsNullOrWhiteSpace(plan.FemaleFertilizedEggId))
            return $"apply: upgrade female produce to fertilized ({plan.FemaleUpgradeChancePercent.ToString(CultureInfo.InvariantCulture)}%) egg={plan.FemaleFertilizedEggId}";

        if (plan.RoosterHatchesFertilizedEgg && !string.IsNullOrWhiteSpace(plan.RoosterFertilizedEggId))
            return $"apply: rooster hatches fertilized egg egg={plan.RoosterFertilizedEggId}";

        if (!plan.RoosterHatchesFertilizedEgg && !string.IsNullOrWhiteSpace(plan.FemaleFertilizedEggId))
            return $"apply: female lays fertilized egg egg={plan.FemaleFertilizedEggId}";

        return "skip: missing fertilized egg id(s)";
    }

    private string BuildDebugPlanText(
        DailyHatchingPlanEnvelope plan,
        IDictionary<string, FarmAnimalData>? farmAnimalsData,
        string? buildingKeyFilter,
        bool full,
        bool adHoc,
        string? locationName
    )
    {
        var dump = BuildDebugPlanDump(plan, farmAnimalsData, buildingKeyFilter, full, adHoc, locationName);

        var sb = new StringBuilder();
        sb.AppendLine("[ra_debug_plan] Plan:");
        sb.AppendLine($"  day={dump.Day} farmId={dump.FarmId} nonce={dump.Nonce} adhoc={dump.AdHoc}");
        sb.AppendLine($"  fingerprint={dump.Fingerprint}");
        if (!string.IsNullOrWhiteSpace(dump.LocationName))
            sb.AppendLine($"  locationName={dump.LocationName}");

        foreach (var b in dump.Buildings)
        {
            sb.AppendLine($"Building: {b.BuildingKey}");
            foreach (var r in b.Roosters)
            {
                sb.AppendLine($"  Rooster: {r.RoosterKey}");
                foreach (var f in r.Females)
                {
                    if (!full)
                    {
                        // In non-full mode, only show applied actions.
                        var actions = new List<string>();

                        if (f.NormalEgg is not null && f.NormalEgg.Decision.StartsWith("apply:", StringComparison.OrdinalIgnoreCase))
                            actions.Add($"normalEgg {f.NormalEgg.Decision}");
                        if (f.LargeEgg is not null && f.LargeEgg.Decision.StartsWith("apply:", StringComparison.OrdinalIgnoreCase))
                            actions.Add($"largeEgg {f.LargeEgg.Decision}");
                        if (f.FertilizedOnly is not null && f.FertilizedOnly.Decision.StartsWith("apply:", StringComparison.OrdinalIgnoreCase))
                            actions.Add($"fertilizedOnly {f.FertilizedOnly.Decision}");

                        if (actions.Count == 0)
                            continue;

                        sb.AppendLine($"    Female: {f.FemaleKey}");
                        foreach (var a in actions)
                            sb.AppendLine($"      {a}");

                        continue;
                    }

                    sb.AppendLine($"    Female: {f.FemaleKey}");

                    if (f.NormalEgg is not null)
                    {
                        sb.AppendLine($"      normalEgg: {f.NormalEgg.Decision}");
                        if (f.NormalEgg.RemovedFromOtherAnimals is not null && f.NormalEgg.RemovedFromOtherAnimals.Count > 0)
                            sb.AppendLine($"        removedFromOtherAnimals: {string.Join(", ", f.NormalEgg.RemovedFromOtherAnimals)}");
                    }

                    if (f.LargeEgg is not null)
                    {
                        sb.AppendLine($"      largeEgg: {f.LargeEgg.Decision}");
                        if (f.LargeEgg.RemovedFromOtherAnimals is not null && f.LargeEgg.RemovedFromOtherAnimals.Count > 0)
                            sb.AppendLine($"        removedFromOtherAnimals: {string.Join(", ", f.LargeEgg.RemovedFromOtherAnimals)}");
                    }

                    if (f.FertilizedOnly is not null)
                        sb.AppendLine($"      fertilizedOnly: {f.FertilizedOnly.Decision}");
                }
            }
        }

        return sb.ToString().TrimEnd();
    }

    private sealed record DebugDiffDump(
        DebugDecisionsDump Previous,
        DebugDecisionsDump Current,
        List<DebugDiffEntry> Changes
    );

    private sealed record DebugDiffEntry(string Path, string Before, string After);

    private static DebugDiffDump BuildDebugDiffDump(DebugDecisionsDump previous, DebugDecisionsDump current)
    {
        var beforeMap = FlattenDecisions(previous);
        var afterMap = FlattenDecisions(current);

        var allKeys = beforeMap.Keys
            .Union(afterMap.Keys, StringComparer.Ordinal)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        var changes = new List<DebugDiffEntry>();
        foreach (var k in allKeys)
        {
            beforeMap.TryGetValue(k, out var b);
            afterMap.TryGetValue(k, out var a);

            b ??= "<missing>";
            a ??= "<missing>";

            if (!string.Equals(b, a, StringComparison.Ordinal))
                changes.Add(new DebugDiffEntry(k, b, a));
        }

        return new DebugDiffDump(previous, current, changes);
    }

    private static Dictionary<string, string> FlattenDecisions(DebugDecisionsDump dump)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Day"] = dump.Day.ToString(CultureInfo.InvariantCulture),
            ["FarmId"] = dump.FarmId.ToString(CultureInfo.InvariantCulture),
            ["Fingerprint"] = dump.Fingerprint ?? string.Empty
        };

        if (dump.Plan is null)
        {
            map["Plan"] = "<null>";
            return map;
        }

        map["Plan.Day"] = dump.Plan.Day.ToString(CultureInfo.InvariantCulture);
        map["Plan.FarmId"] = dump.Plan.FarmId.ToString(CultureInfo.InvariantCulture);
        map["Plan.Fingerprint"] = dump.Plan.Fingerprint ?? string.Empty;
        map["Plan.Nonce"] = dump.Plan.Nonce.ToString(CultureInfo.InvariantCulture);
        map["Plan.AdHoc"] = dump.Plan.AdHoc ? "true" : "false";

        foreach (var b in dump.Plan.Buildings)
        {
            foreach (var r in b.Roosters)
            {
                foreach (var f in r.Females)
                {
                    var basePath = $"Buildings[{b.BuildingKey}]/Roosters[{r.RoosterKey}]/Females[{f.FemaleKey}]";

                    if (f.NormalEgg is not null)
                    {
                        map[$"{basePath}/NormalEgg.Decision"] = f.NormalEgg.Decision ?? string.Empty;
                        map[$"{basePath}/NormalEgg.ChancePercent"] = f.NormalEgg.ChancePercent.ToString(CultureInfo.InvariantCulture);
                        map[$"{basePath}/NormalEgg.RolledSuccess"] = f.NormalEgg.RolledSuccess ? "true" : "false";
                        if (!string.IsNullOrWhiteSpace(f.NormalEgg.EggIdToAddToRooster))
                            map[$"{basePath}/NormalEgg.EggIdToAdd"] = f.NormalEgg.EggIdToAddToRooster;
                    }
                    else
                    {
                        map[$"{basePath}/NormalEgg"] = "<null>";
                    }

                    if (f.LargeEgg is not null)
                    {
                        map[$"{basePath}/LargeEgg.Decision"] = f.LargeEgg.Decision ?? string.Empty;
                        map[$"{basePath}/LargeEgg.ChancePercent"] = f.LargeEgg.ChancePercent.ToString(CultureInfo.InvariantCulture);
                        map[$"{basePath}/LargeEgg.RolledSuccess"] = f.LargeEgg.RolledSuccess ? "true" : "false";
                        if (!string.IsNullOrWhiteSpace(f.LargeEgg.EggIdToAddToRooster))
                            map[$"{basePath}/LargeEgg.EggIdToAdd"] = f.LargeEgg.EggIdToAddToRooster;
                    }
                    else
                    {
                        map[$"{basePath}/LargeEgg"] = "<null>";
                    }

                    if (f.FertilizedOnly is not null)
                    {
                        map[$"{basePath}/FertilizedOnly.Decision"] = f.FertilizedOnly.Decision ?? string.Empty;
                        map[$"{basePath}/FertilizedOnly.UpgradeFemaleProduce"] = f.FertilizedOnly.UpgradeFemaleProduceToFertilized ? "true" : "false";
                        map[$"{basePath}/FertilizedOnly.RoosterHatches"] = f.FertilizedOnly.RoosterHatchesFertilizedEgg ? "true" : "false";
                        if (!string.IsNullOrWhiteSpace(f.FertilizedOnly.RoosterFertilizedEggId))
                            map[$"{basePath}/FertilizedOnly.RoosterEggId"] = f.FertilizedOnly.RoosterFertilizedEggId;
                        if (!string.IsNullOrWhiteSpace(f.FertilizedOnly.FemaleFertilizedEggId))
                            map[$"{basePath}/FertilizedOnly.FemaleEggId"] = f.FertilizedOnly.FemaleFertilizedEggId;
                    }
                    else
                    {
                        map[$"{basePath}/FertilizedOnly"] = "<null>";
                    }
                }
            }
        }

        return map;
    }

    private static string BuildDebugDiffText(DebugDiffDump diff)
    {
        var sb = new StringBuilder();

        sb.AppendLine("[ra_debug_diff] Previous:");
        sb.AppendLine($"  day={diff.Previous.Day} farmId={diff.Previous.FarmId} fingerprint={diff.Previous.Fingerprint}");
        sb.AppendLine("[ra_debug_diff] Current:");
        sb.AppendLine($"  day={diff.Current.Day} farmId={diff.Current.FarmId} fingerprint={diff.Current.Fingerprint}");
        sb.AppendLine("[ra_debug_diff] Changes:");

        if (diff.Changes.Count == 0)
        {
            sb.AppendLine("  <none>");
            return sb.ToString().TrimEnd();
        }

        foreach (var c in diff.Changes)
            sb.AppendLine($"  {c.Path}: {c.Before} -> {c.After}");

        return sb.ToString().TrimEnd();
    }
}
#endif
