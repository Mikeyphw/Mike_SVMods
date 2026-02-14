using StardewValley;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Enums;

namespace Mikew.BirdsOfAFeather.Helpers
{
    public static class DailyLuckHelper
    {
        // Conditions used by the Content Patcher API
        // Condition for super lucky state

        public static LuckyTypes GetDailyLuck()
        {
            return Game1.player.DailyLuck switch
            {
                < -0.07 => LuckyTypes.SuperUnlucky,
                >= -0.07 and < -0.02 => LuckyTypes.Unlucky,
                >= -0.02 and < 0.02 => LuckyTypes.Neutral,
                >= 0.02 and < 0.07 => LuckyTypes.Lucky,
                >= 0.07 => LuckyTypes.SuperLucky,
                _ => LuckyTypes.Neutral,
            };
        }

        public static double AdjustChanceByDailyLuck(double chance, double dailyLuckModifier)
        {
            var luckAdjustment = GetDailyLuck() switch
            {
                LuckyTypes.SuperUnlucky => -2 * dailyLuckModifier,
                LuckyTypes.Neutral => 0,
                LuckyTypes.Unlucky => -dailyLuckModifier,
                LuckyTypes.Lucky => dailyLuckModifier,
                LuckyTypes.SuperLucky => +1.5 * dailyLuckModifier,
                _ => 0, // Default case (unknown luck type)
            };
            var finalChance = chance + luckAdjustment;
            return Math.Clamp(finalChance, 0, 1);
        }
    }
}
