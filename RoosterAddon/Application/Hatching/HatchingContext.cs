using System;
using RoosterAddon.Framework.Config;
using RoosterAddon.Helpers;
using RoosterAddon.Enums;
using RoosterAddon.Infrastructure.SaveData;

using ILetMeHatchApi = Mikew.BirdsOfAFeather.Framework.ILetMeHatchApi;

namespace RoosterAddon.Application.Hatching;

/// <summary>
/// Immutable inputs used by hatching services. This keeps hatching logic mostly pure and testable.
/// </summary>
internal sealed class HatchingContext
{
    public RoosterAddonConfig Config { get; }
    public RoosterControl RoosterHelper { get; }
    public AnimalExclusion AnimalExclusions { get; }
    public ILetMeHatchApi? HatchApi { get; }

    public HatchingContext(
        RoosterAddonConfig config,
        RoosterControl roosterHelper,
        AnimalExclusion animalExclusions,
        ILetMeHatchApi? hatchApi
    )
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        RoosterHelper = roosterHelper ?? throw new ArgumentNullException(nameof(roosterHelper));
        AnimalExclusions = animalExclusions ?? throw new ArgumentNullException(nameof(animalExclusions));
        HatchApi = hatchApi;
    }

    public bool IsFertilizedOnly => Config.General.HatchingMode == HatchingMode.OnlyFertilizedEggs;

    public bool EnableEggProbability => Config.General.EnableEggProbability;
    public bool EnableLargeEggProbability => Config.General.EnableLargeEggProbability;

    public int RoosterToChickenRatio => Config.General.RoosterToChickenRatio;

    public static bool RollPercentage(int chance) => RoosterControl.UpdatePercentage(chance);

    public static bool RollPercentage(float chance) => RoosterControl.UpdatePercentage(chance);
}
