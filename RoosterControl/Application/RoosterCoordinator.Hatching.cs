using System;
using System.Collections.Generic;
using Mikew.NestCore.Helpers.GameStateQueries;
using RoosterControl.Application.Hatching;
using RoosterControl.Application.Planning;
using RoosterControl.Enums;
using RoosterControl.Framework.Config;
using RoosterControl.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using Mikew.NestCore.Helpers;

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
        EnsureRoostersSetup();

        DailyLuck = DailyLuckHelper.GetDailyLuck();
        RoosterHelper.DailyLuck = DailyLuck;

        isStuckInside = EvaluateStuckInsideWithGsq();

        if (Config.General.UpdateRate == UpdateRateMode.OnDayStart)
        {
            InvalidateRuleCaches("update rate: day start");
            _ = Helper.GameContent.InvalidateCache(FarmAnimalsAssetName);
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

    internal void GetRoostersFemales(
        IDictionary<string, FarmAnimalData> animalData,
        Dictionary<RoosterType, RoosterData> roosters)
    {
        var plan = FemaleAssignmentService.ComputeFemaleAssignmentsPlan(
            config: Config,
            animalData: animalData,
            roosters: roosters,
            animalExclusions: _animalExclusions,
            hatchApi: HatchApi
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

        _ = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
        ReloadCoopRoosters();

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

        var data = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
        if (data is null || data.Count == 0)
            return;

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

        _ = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
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
