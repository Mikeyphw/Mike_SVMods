using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Monsters.Base;
using static WildEggsHatching.EnumsAndStrings.Enums;

namespace WildEggsHatching.DesertTomb.Utilities
{
    /// <summary>
    /// Utility class that provides the old GetModifiedMonsterItems functionality
    /// for use with the refactored DifficultyManager system
    /// </summary>
    public static class MonsterItemHelper
    {
        /// <summary>
        /// Gets modified monster items for a given monster instance
        /// </summary>
        /// <param name="monster">The monster to get items for</param>
        /// <param name="extraItems">Optional extra items string</param>
        /// <returns>List of item strings</returns>
        public static List<string> GetModifiedMonsterItems(
            Monster monster,
            string? extraItems = null
        )
        {
            var monsterType = TombMonsterUtilities.GetMonsterType(monster);
            return GetModifiedMonsterItems(monsterType, extraItems);
        }

        /// <summary>
        /// Gets modified monster items for a given monster type
        /// </summary>
        /// <param name="monsterType">The type of monster</param>
        /// <param name="extraItems">Optional extra items string</param>
        /// <returns>List of item strings</returns>
        public static List<string> GetModifiedMonsterItems(
            TombMonsterType monsterType,
            string? extraItems = null
        )
        {
            var itemsWithChance = TombMonsterUtilities.MonsterDropItems[monsterType];
            if (extraItems != null)
            {
                itemsWithChance = $"{itemsWithChance} {extraItems}";
            }
            var chosenItems = GetExtraItemsList(itemsWithChance);
            var items = new List<string>();
            AddExtraItemsWithLuckyBonus(items, chosenItems);
            items = items.Take(4).ToList();
            return items;
        }

        /// <summary>
        /// Parses extra items string into a list of tuples
        /// </summary>
        /// <param name="itemsString">String format: "item1 chance1 item2 chance2"</param>
        /// <returns>List of (item, chance) tuples</returns>
        private static List<(string, double)> GetExtraItemsList(string itemsString)
        {
            var parts = itemsString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var list = new List<(string, double)>();

            for (int i = 0; i < parts.Length; i += 2)
            {
                if (i + 1 < parts.Length)
                {
                    var key = parts[i];
                    if (double.TryParse(parts[i + 1], out var value))
                    {
                        list.Add((key, value));
                    }
                }
            }
            return list.OrderBy(x => Game1.random.Next()).ToList();
        }

        /// <summary>
        /// Adds extra items to the list based on their chance and lucky bonus
        /// </summary>
        /// <param name="items">The list to add items to</param>
        /// <param name="extraItems">Items with their chances</param>
        /// <returns>The updated items list</returns>
        private static List<string> AddExtraItemsWithLuckyBonus(
            List<string> items,
            List<(string, double)> extraItems
        )
        {
            foreach (var (item, chance) in extraItems)
            {
                var roll = Game1.random.NextDouble();
                var newChance = LuckyBonusCalculator(chance);

                if (roll < newChance)
                {
                    items.Add(item);
                }
            }
            return items;
        }

        /// <summary>
        /// Calculates lucky bonus for drop chances
        /// </summary>
        /// <param name="originalValue">Original drop chance</param>
        /// <returns>Adjusted drop chance with lucky bonus</returns>
        private static double LuckyBonusCalculator(double originalValue)
        {
            var currentLuck = GetLuckyType(Game1.player.DailyLuck);
            Func<double, double> luckyBonus = currentLuck switch
            {
                LuckyTypes.SuperUnlucky => (value) => value * 0,
                LuckyTypes.Unlucky => (value) => value - 0.001,
                LuckyTypes.Neutral => (value) => value,
                LuckyTypes.Lucky => (value) => value + 0.001,
                LuckyTypes.SuperLucky => (value) => value + 0.01,
                _ => (value) => value * 0,
            };
            var luckyBonus2 = originalValue * (1 + Game1.player.team.AverageDailyLuck());
            return Math.Clamp(Math.Min(luckyBonus(originalValue), luckyBonus2), 0, 1);
        }

        /// <summary>
        /// Gets the lucky type based on daily luck value
        /// </summary>
        /// <param name="dailyLuck">Daily luck value</param>
        /// <returns>Lucky type enum</returns>
        private static LuckyTypes GetLuckyType(double dailyLuck)
        {
            return dailyLuck switch
            {
                < -0.07 => LuckyTypes.SuperUnlucky,
                >= -0.07 and <= -0.02 => LuckyTypes.Unlucky,
                > -0.02 and <= 0.02 => LuckyTypes.Neutral,
                > 0.02 and <= 0.07 => LuckyTypes.Lucky,
                > 0.07 => LuckyTypes.SuperLucky,
                _ => LuckyTypes.Neutral,
            };
        }

        /// <summary>
        /// Lucky types for drop chance calculations
        /// </summary>
        private enum LuckyTypes
        {
            SuperUnlucky,
            Unlucky,
            Neutral,
            Lucky,
            SuperLucky,
        }
    }
}
