using System;

namespace RoosterAddon.Enums
{
    public enum OptionDetails
    {
        Value,
        Type,
        AllowedValues,
        Max,
        Min,
        Interval,
    }

    public enum OptionEntries
    {
        UpdateRate,
        ChosenPercentage_DailyLuckBonus,
        HatchingMode,
        BetterTrufflesCompat,
        EnableStuckInside,
        EnableEggProbability,
        EnableLargeEggProbability,
        EnableDebug,
        HarvestType,
        EggProbability,
        LargeEggProbability,
        FertilizedEggProbability,
        BoostByRooster,
        ProduceOption,
        DeluxeProduceOption,
        BypassBlueConditions,
        MatureDays,
        ProduceDays,
        BuildingRequired,
        Price,
        Buyable,
        EnableRoosterHatching,
        EditAllFemales,
        RoosterToChickenRatio,
        BlueEggChoice,
        Produce_1,
        Produce_2,
        Produce_3,
        Produce_4,
        Produce_1_Prob,
        Produce_2_Prob,
        Produce_3_Prob,
        Produce_4_Prob,
        DeluxeProduce_1,
        DeluxeProduce_2,
        DeluxeProduce_3,
        DeluxeProduce_4,
        DeluxeProduce_1_Prob,
        DeluxeProduce_2_Prob,
        DeluxeProduce_3_Prob,
        DeluxeProduce_4_Prob,
        CustomProduce_1,
        CustomProduce_2,
        CustomProduce_3,
        CustomProduce_4,
        CustomDeluxeProduce_1,
        CustomDeluxeProduce_2,
        CustomDeluxeProduce_3,
        CustomDeluxeProduce_4
    }

    [Obsolete("Use Mikew.BirdsOfAFeather.EnumsAndStrings.Enums.LuckyTypes instead.")]
    public enum LuckyTypes
    {
        SuperUnlucky,
        Unlucky,
        Neutral,
        Lucky,
        SuperLucky
    }
}
