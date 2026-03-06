using System;
using System.Collections.Generic;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Helpers.GameStateQueries;
using RoosterControl.Application.Hatching;
using RoosterControl.Application.Planning;
using RoosterControl.Application.Compat;
using RoosterControl.Application.FertilizedEggs;
using RoosterControl.Enums;
using RoosterControl.Framework.Config;
using RoosterControl.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.FarmAnimals;

namespace RoosterControl.Application;

internal sealed partial class RoosterCoordinator
{
    private DailyFarmAnimalsDecisions? _dailyDecisions;
    private DailyFarmAnimalsDecisions? _previousDailyDecisions;

    public void OnDayStarted(object? sender, DayStartedEventArgs e)
        => OnDayStart(sender, e);

    /// <summary>The method called after a new day starts.</summary>
    private void OnDayStart(object? sender, DayStartedEventArgs e)
    {
        HasDayStarted = true;

        InvalidateDailyPlanCache("day started");

        DailyLuck = DailyLuckHelper.GetDailyLuck();
        RoosterHelper.DailyLuck = DailyLuck;

        EnsureRoostersSetup();

        isStuckInside = EvaluateStuckInsideWithGsq();

        if (Config.General.UpdateRate == UpdateRateMode.OnDayStart)
        {
            RequestInvalidate(FarmAnimalsAssetName);
            _invalidations.Flush();
        }

        RunFarmAnimalsDailySystems();

        RequestGmcmRebuild();
    }

    private void EnsureRoostersSetup()
    {
        if (!Context.IsWorldReady)
            return;

        var today = SDate.Now().DaysSinceStart;
        if (_needsRoosterSetup || _lastRoosterSetupDay != today)
        {
            RoosterHelper.SetUpRoosters();
            _lastRoosterSetupDay = today;
            _needsRoosterSetup = false;
        }
    }
    private void ApplyCompatSelectionsToRuntime()
    {
        if (!Context.IsWorldReady)
            return;

        if (!HasDayStarted)
            return;

        EnsureCompatInitialized();

        _invalidations.Flush();

        Dictionary<string, FarmAnimalData>? animalData;
        try
        {
            animalData = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
        }
        catch
        {
            return;
        }

        if (animalData is null)
            return;

        ApplyCompatSelectionsToRuntime(animalData);
    }

    private void ApplyCompatSelectionsToRuntime(IDictionary<string, FarmAnimalData> animalData)
    {
        if (!Context.IsWorldReady)
            return;

        if (!HasDayStarted)
            return;

        EnsureCompatInitialized();

        if (animalData is null || animalData.Count == 0)
            return;

        var enabledKeys = _compatSelection.GetEnabledRoosterFarmAnimalKeys(_saveState.Compat);
        RoosterHelper.SetCoopRoosterFarmAnimalKeys(enabledKeys);

        foreach (var pair in RoosterHelper.Roosters)
        {
            var roosterType = pair.Key;
            var rooster = pair.Value;

            if (!TryGetEggKindsForRoosterType(roosterType, out var normalKind, out var largeKind, out var fertKind))
                continue;

            string? resolvedNormalId = null;
            if (rooster.Eggs.TryGetValue(EggTypeEntries.Normal, out var normalEgg)
                && _compatSelection.TryResolveSelectedEggItemId(_saveState.Compat, normalKind, animalData, out var normalId, out _)
                && !string.IsNullOrWhiteSpace(normalId))
            {
                resolvedNormalId = normalId;
                normalEgg[RoosterItemDataEntries.Name] = normalId;
            }

            string? resolvedLargeId = null;
            if (rooster.Eggs.TryGetValue(EggTypeEntries.Large, out var largeEgg)
                && _compatSelection.TryResolveSelectedEggItemId(_saveState.Compat, largeKind, animalData, out var largeId, out _)
                && !string.IsNullOrWhiteSpace(largeId))
            {
                resolvedLargeId = largeId;
                largeEgg[RoosterItemDataEntries.Name] = largeId;
            }

            if (rooster.Eggs.TryGetValue(EggTypeEntries.Fertilized, out var fertilizedEgg))
            {
                var fertSourceId = _saveState.Compat.SelectedEggSources.TryGetValue(fertKind, out var fs) ? fs : null;
                var normalSourceId = _saveState.Compat.SelectedEggSources.TryGetValue(normalKind, out var ns) ? ns : null;
                var largeSourceId = _saveState.Compat.SelectedEggSources.TryGetValue(largeKind, out var ls) ? ls : null;

                var useNativeFertilized = !string.IsNullOrWhiteSpace(fertSourceId)
                    && ((!string.IsNullOrWhiteSpace(largeSourceId) && fertSourceId.Equals(largeSourceId, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(normalSourceId) && fertSourceId.Equals(normalSourceId, StringComparison.OrdinalIgnoreCase)));

                if (useNativeFertilized
                    && _compatSelection.TryResolveSelectedEggItemId(_saveState.Compat, fertKind, animalData, out var fertId, out _)
                    && !string.IsNullOrWhiteSpace(fertId))
                {
                    fertilizedEgg[RoosterItemDataEntries.Name] = fertId;
                }
                else
                {
                    var baseEggId = !string.IsNullOrWhiteSpace(resolvedLargeId) ? resolvedLargeId : resolvedNormalId;
                    if (!string.IsNullOrWhiteSpace(baseEggId))
                        fertilizedEgg[RoosterItemDataEntries.Name] = FertilizedEggsObjectEditService.GetSynthesizedFertilizedId(baseEggId);
                }
            }

            RoosterHelper.UpdateFemaleEggs(rooster, animalData);

            if (rooster.Eggs.TryGetValue(EggTypeEntries.Fertilized, out var fertEggEntry)
                && fertEggEntry.TryGetValue(RoosterItemDataEntries.Name, out var fertEggId)
                && !string.IsNullOrWhiteSpace(fertEggId))
            {
                RoosterHelper.FemaleEggs.TryAdd(rooster.Female, new Dictionary<EggTypeEntries, string>());
                RoosterHelper.FemaleEggs[rooster.Female][EggTypeEntries.Fertilized] = fertEggId;
            }
        }
    }

    private static bool TryGetEggKindsForRoosterType(
        RoosterType roosterType,
        out EggKind normal,
        out EggKind large,
        out EggKind fertilized
    )
    {
        switch (roosterType)
        {
            case RoosterType.Brown:
                normal = EggKind.BrownEgg;
                large = EggKind.LargeBrownEgg;
                fertilized = EggKind.FertilizedBrownEgg;
                return true;

            case RoosterType.White:
                normal = EggKind.WhiteEgg;
                large = EggKind.LargeWhiteEgg;
                fertilized = EggKind.FertilizedWhiteEgg;
                return true;

            case RoosterType.Blue:
                normal = EggKind.BlueEgg;
                large = EggKind.LargeBlueEgg;
                fertilized = EggKind.FertilizedBlueEgg;
                return true;

            case RoosterType.Void:
                normal = EggKind.VoidEgg;
                large = EggKind.LargeVoidEgg;
                fertilized = EggKind.FertilizedVoidEgg;
                return true;

            case RoosterType.Gold:
                normal = EggKind.GoldEgg;
                large = EggKind.LargeGoldEgg;
                fertilized = EggKind.FertilizedGoldEgg;
                return true;

            default:
                normal = default;
                large = default;
                fertilized = default;
                return false;
        }
    }

    internal void GetRoostersFemales(
        IDictionary<string, FarmAnimalData> animalData,
        Dictionary<RoosterType, RoosterData> roosters)
    {
        var plan = FemaleAssignmentService.ComputeFemaleAssignmentsPlan(
            config: Config,
            animalData: animalData,
            roosters: roosters,
            animalExclusions: _animalExclusions,
            hatchApi: HatchApi,
            wildEggsApi: WildEggsApi
        );

        FemaleAssignmentService.ApplyFemaleAssignmentsPlan(Config, RoosterHelper, plan);

        if (plan.ConfigChanged)
            _femaleAssignmentsDirty = true;
    }

    private void UpdateRoosterHatchingApi()
    {
        FemaleAssignmentService.UpdateChosenFemalesApi(Config);
    }

    private void RunHatchingSystem(
        RoosterData rooster,
        IDictionary<string, FarmAnimalData> data,
        DailyHatchingPlanEnvelope? plan)
    {
        var ctx = new HatchingContext(Config, RoosterHelper, _animalExclusions, HatchApi);

        if (plan is null)
        {
            _farmAnimalHatching.ApplyForRooster(ctx, rooster, data);
            return;
        }

        var buildingKey = ResolveBuildingKey(rooster, data);
        var otherAnimals = GetOtherAnimalsSharingEggsCached(data, rooster, plan);

        _planApplier.Apply(
            plan: plan,
            buildingKey: buildingKey,
            rooster: rooster,
            data: data,
            otherAnimalsSharingEggs: otherAnimals
        );
    }

    private bool TryApplyDailyFarmAnimalsEdits(IDictionary<string, FarmAnimalData> data)
    {
        if (!Context.IsWorldReady)
            return false;

        if (_dailyDecisions is null)
            return false;

        var today = SDate.Now().DaysSinceStart;
        var farmId = unchecked((long)Game1.uniqueIDForThisGame);
        if (!_dailyDecisions.MatchesToday(farmId, today))
            return false;

        var plan = _dailyDecisions.Plan;

        foreach (var rooster in RoosterHelper.Roosters.Values)
            RunHatchingSystem(rooster, data, plan);

        return true;
    }

    private void RefreshFarmAnimalsAfterDailyDecisions()
    {
        if (!Context.IsWorldReady)
            return;

        RefreshFarmAnimalsOnLocationChange();

        try
        {
            var data = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
            if (data is not null)
                DebugDumpFarmAnimalsIfEnabled(new Dictionary<string, FarmAnimalData>(data));
        }
        catch
        {
            // best effort
        }
    }

    private void RunFarmAnimalsDailySystems()
    {
        if (!Context.IsWorldReady)
            return;

        EnsureRoostersSetup();

        _invalidations.Flush();

        Dictionary<string, FarmAnimalData>? data;
        try
        {
            data = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
        }
        catch
        {
            return;
        }

        if (data is null || data.Count == 0)
            return;

        ApplyCompatSelectionsToRuntime(data);

        RoosterHelper.UpdateRoosterQuantity_All();
        GetRoostersFemales(data, RoosterHelper.Roosters);
        UpdateRoosterHatchingApi();

        var plan = EnsureDailyPlanForToday(
            farmAnimalsData: data,
            forceRecompute: false,
            usedHostPlan: out _
        );

        var today = SDate.Now().DaysSinceStart;
        var farmId = unchecked((long)Game1.uniqueIDForThisGame);

        _previousDailyDecisions = _dailyDecisions;
        _dailyDecisions = new DailyFarmAnimalsDecisions(today, farmId, _dailyPlanFingerprint, plan);

        // Apply the plan through the asset editor (single source of truth).
        RequestInvalidate(FarmAnimalsAssetName);
        if (RoosterHelper.FemaleEggs.Count > 0)
            RequestInvalidate(ObjectsAssetName);

        QueueRefreshFarmAnimalsAfterDailyDecisions();
    }

    private void RefreshFarmAnimalsOnLocationChange()
    {
        if (!Context.IsWorldReady)
            return;

        _invalidations.Flush();

        Dictionary<string, FarmAnimalData>? data;
        try
        {
            data = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
        }
        catch
        {
            return;
        }

        if (data is null || data.Count == 0)
            return;

        EnsureRoostersSetup();
        ApplyCompatSelectionsToRuntime(data);
        ReloadCoopRoosters();
    }

    private static void ReloadCoopRoosters()
    {
        foreach (var roosterFarmAnimal in RoosterHelper.GetRoostersFromCoops())
            roosterFarmAnimal.reload(roosterFarmAnimal.home);
    }

    private bool GetHatchApiCloneEggsOption()
        => HatchApi is not null
            && (HatchApi.GetClonedAnimalsInfo().Count > 0 || HatchApi.AnimalsWithClonedEggs.Count > 0);

    private static bool EvaluateStuckInsideWithGsq()
    {
        var stuckInsideQuery = string.Join(
            ", ",
            TimeCondition.Season("winter"),
            WorldCondition.Weather(LocationType.Here, "rain"),
            WorldCondition.Weather(LocationType.Here, "storm"),
            WorldCondition.Weather(LocationType.Here, "snow"),
            WorldCondition.Weather(LocationType.Here, "greenrain")
        );

        try
        {
            return GameStateQuery.CheckConditions(stuckInsideQuery);
        }
        catch
        {
            return string.Equals(Game1.currentSeason, "winter", StringComparison.OrdinalIgnoreCase)
                || Game1.isRaining
                || Game1.isLightning
                || Game1.isSnowing;
        }
    }
}
