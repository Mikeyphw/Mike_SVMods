using System;
using System.Collections.Generic;
using Mikew.BirdsOfAFeather.Helpers;
using RoosterAddon.Application.Hatching;
using RoosterAddon.Enums;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

using LuckyTypes = Mikew.BirdsOfAFeather.EnumsAndStrings.Enums.LuckyTypes;

namespace RoosterAddon.Application;

internal sealed partial class RoosterCoordinator
{
    public void OnDayStarted(object? sender, DayStartedEventArgs e) => OnDayStart(sender, e);

    /// <summary>The method called after a new day starts.</summary>
    private void OnDayStart(object? sender, DayStartedEventArgs e)
    {
        HasDayStarted = true;

        InvalidateDailyPlanCache("day started");
        EnsureRoostersSetup();

        DailyLuck = DailyLuckHelper.GetDailyLuck();
        RoosterHelper.DailyLuck = DailyLuck;

        // Evaluate "stuck inside" using CP when available, otherwise fall back to vanilla state.
        isStuckInside = false;
        if (ModEntry.ContentPatcher is not null)
        {
            var rawStuckInsideSeason = new Dictionary<string, string?>
            {
                ["Season |contains=winter"] = "true",
            };
            var rawStuckInsideWeather = new Dictionary<string, string?>
            {
                ["Weather |contains=rain, storm, snow, greenrain"] = "true",
            };

            conditionStuckInsideSeason = new Mikew.BirdsOfAFeather.Framework.ManagedConditions(
                ModEntry.ContentPatcher.ParseConditions(
                    manifest: ModManifest,
                    rawConditions: rawStuckInsideSeason,
                    formatVersion: new SemanticVersion("2.0.0")
                )
            );
            conditionStuckInsideWeather = new Mikew.BirdsOfAFeather.Framework.ManagedConditions(
                ModEntry.ContentPatcher.ParseConditions(
                    manifest: ModManifest,
                    rawConditions: rawStuckInsideWeather,
                    formatVersion: new SemanticVersion("2.0.0")
                )
            );

            conditionStuckInsideSeason.UpdateContext();
            conditionStuckInsideWeather.UpdateContext();
            isStuckInside = conditionStuckInsideSeason.IsMatch || conditionStuckInsideWeather.IsMatch;
        }
        else
        {
            isStuckInside =
                string.Equals(Game1.currentSeason, "winter", StringComparison.OrdinalIgnoreCase)
                || Game1.isRaining
                || Game1.isLightning
                || Game1.isSnowing;
        }

        if (Config.General.UpdateRate == UpdateRateMode.OnDayStart)
            _ = Helper.GameContent.InvalidateCache(FarmAnimalsAssetName);
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
        IDictionary<string, StardewValley.GameData.FarmAnimals.FarmAnimalData> animalData,
        Dictionary<RoosterType, RoosterData> roosters
    )
    {
        var changed = FemaleAssignmentService.SyncFemaleAssignments(
            config: Config,
            roosterHelper: RoosterHelper,
            animalData: animalData,
            roosters: roosters,
            animalExclusions: _animalExclusions,
            hatchApi: HatchApi
        );

        if (changed)
            _femaleAssignmentsDirty = true;
    }

    private void UpdateRoosterHatchingApi()
    {
        FemaleAssignmentService.UpdateChosenFemalesApi(Config);
    }

    private void RunHatchingSystem(
        RoosterData rooster,
        IDictionary<string, StardewValley.GameData.FarmAnimals.FarmAnimalData> data
    )
    {
        var ctx = new HatchingContext(Config, RoosterHelper, _animalExclusions, HatchApi);

        var plan = EnsureDailyPlanForToday(
            farmAnimalsData: data,
            forceRecompute: false,
            usedHostPlan: out _
        );

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

    private bool GetHatchApiCloneEggsOption()
        => HatchApi is not null && HatchApi.GetClonedAnimalsInfo().Count > 0;
}
