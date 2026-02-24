using System;
using RoosterControl.Framework;
using RoosterControl.Framework.Config;
using RoosterControl.Helpers;
using RoosterControl.Enums;
using RoosterControl.Infrastructure.SaveData;
using RoosterControl.Application.Hatching.Decisions;

using ILetMeHatchApi = Mikew.NestCore.Framework.ILetMeHatchApi;

namespace RoosterControl.Application.Hatching;

/// <summary>
/// Immutable inputs used by hatching services. This keeps hatching logic mostly pure and testable.
/// </summary>
internal sealed class HatchingContext
{
    public RoosterControlConfig Config { get; }
    public RoosterHelper RoosterHelper { get; }
    public AnimalExclusion AnimalExclusions { get; }
    public ILetMeHatchApi? HatchApi { get; }
    public IPercentageRoller Roller { get; }

    public HatchingContext(
        RoosterControlConfig config,
        RoosterHelper roosterHelper,
        AnimalExclusion animalExclusions,
        ILetMeHatchApi? hatchApi,
        IPercentageRoller? roller = null
    )
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        RoosterHelper = roosterHelper ?? throw new ArgumentNullException(nameof(roosterHelper));
        AnimalExclusions = animalExclusions ?? throw new ArgumentNullException(nameof(animalExclusions));
        HatchApi = hatchApi;
        Roller = roller ?? new RoosterPercentageRoller();
    }

    public bool IsFertilizedOnly => Config.General.HatchingMode == HatchingMode.OnlyFertilizedEggs;

    public bool EnableEggProbability => Config.General.EnableEggProbability;
    public bool EnableLargeEggProbability => Config.General.EnableLargeEggProbability;

    public int RoosterToChickenRatio => Config.General.RoosterToChickenRatio;

    public static bool RollPercentage(int chance) => Helpers.RoosterHelper.UpdatePercentage(chance);

    public static bool RollPercentage(float chance) => Helpers.RoosterHelper.UpdatePercentage(chance);
}
