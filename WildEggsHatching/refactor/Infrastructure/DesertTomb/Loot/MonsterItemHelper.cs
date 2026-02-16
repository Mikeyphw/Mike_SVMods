using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Monsters;
using WildEggsHatching.Infrastructure.DesertTomb.Monsters.Base;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.Infrastructure.DesertTomb.Loot;

/// <summary>
/// Legacy-compatible helper for producing SDV monster drop item strings.
/// </summary>
public static class MonsterItemHelper
{
    public static List<string> GetModifiedMonsterItems(
        Monster monster,
        bool luckyBonusEnabled,
        string? extraItems = null
    )
    {
        var monsterType = TombMonsterUtilities.GetMonsterType(monster);
        return GetModifiedMonsterItems(monsterType, luckyBonusEnabled, extraItems);
    }

    public static List<string> GetModifiedMonsterItems(
        TombMonsterType monsterType,
        bool luckyBonusEnabled,
        string? extraItems = null
    )
    {
        if (!TombMonsterUtilities.MonsterDropItems.TryGetValue(monsterType, out var itemsWithChance))
            itemsWithChance = string.Empty;

        if (!string.IsNullOrWhiteSpace(extraItems))
            itemsWithChance = string.IsNullOrWhiteSpace(itemsWithChance) ? extraItems : $"{itemsWithChance} {extraItems}";

        if (string.IsNullOrWhiteSpace(itemsWithChance))
            return new List<string>();

        var chosenItems = ParseItems(itemsWithChance);
        var items = new List<string>();

        AddItemsByChance(items, chosenItems, luckyBonusEnabled);

        // Keep the legacy cap.
        return items.Take(4).ToList();
    }

    private static List<(string Item, double Chance)> ParseItems(string itemsString)
    {
        var parts = itemsString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var list = new List<(string, double)>();

        for (var i = 0; i < parts.Length; i += 2)
        {
            if (i + 1 >= parts.Length)
                break;

            var key = parts[i];
            if (double.TryParse(parts[i + 1], out var value))
                list.Add((key, value));
        }

        return list.OrderBy(_ => Game1.random.Next()).ToList();
    }

    private static void AddItemsByChance(
        List<string> items,
        List<(string Item, double Chance)> candidates,
        bool luckyBonusEnabled
    )
    {
        foreach (var (item, chance) in candidates)
        {
            var roll = Game1.random.NextDouble();
            var adjusted = luckyBonusEnabled ? ApplyLuckyBonus(chance) : chance;

            if (roll < adjusted)
                items.Add(item);
        }
    }

    private static double ApplyLuckyBonus(double originalValue)
    {
        var currentLuck = GetLuckyType(Game1.player.DailyLuck);

        Func<double, double> luckyBonus = currentLuck switch
        {
            LuckyTypes.SuperUnlucky => value => value * 0,
            LuckyTypes.Unlucky => value => value - 0.001,
            LuckyTypes.Neutral => value => value,
            LuckyTypes.Lucky => value => value + 0.001,
            LuckyTypes.SuperLucky => value => value + 0.01,
            _ => value => value * 0,
        };

        var luckyBonus2 = originalValue * (1 + Game1.player.team.AverageDailyLuck());
        return Math.Clamp(Math.Min(luckyBonus(originalValue), luckyBonus2), 0, 1);
    }

    private static LuckyTypes GetLuckyType(double dailyLuck) =>
        dailyLuck switch
        {
            < -0.07 => LuckyTypes.SuperUnlucky,
            >= -0.07 and <= -0.02 => LuckyTypes.Unlucky,
            > -0.02 and <= 0.02 => LuckyTypes.Neutral,
            > 0.02 and <= 0.07 => LuckyTypes.Lucky,
            > 0.07 => LuckyTypes.SuperLucky,
            _ => LuckyTypes.Neutral,
        };

    private enum LuckyTypes
    {
        SuperUnlucky,
        Unlucky,
        Neutral,
        Lucky,
        SuperLucky,
    }
}
