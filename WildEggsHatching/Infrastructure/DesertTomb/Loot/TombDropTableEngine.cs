using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using WildEggsHatching.DesertTomb.Core.Model.Loot;

namespace WildEggsHatching.Infrastructure.DesertTomb.Loot;

/// <summary>
/// Rolls <see cref="TombDropTableDefinition"/> into item IDs suitable for <c>Monster.objectsToDrop</c>.
/// </summary>
public sealed class TombDropTableEngine
{
    /// <summary>
    /// Roll a drop table into item IDs. Quantity is expanded into repeated item IDs for compatibility
    /// with SDV's <c>Monster.objectsToDrop</c> list, which is item-id based.
    /// </summary>
    public IReadOnlyList<string> RollToItemIds(
        TombDropTableDefinition table,
        Random rng,
        TombLuckContext luck
    )
    {
        if (table is null)
            throw new ArgumentNullException(nameof(table));
        if (rng is null)
            throw new ArgumentNullException(nameof(rng));

        if (table.MaxItems <= 0 || table.Entries.Count == 0)
            return Array.Empty<string>();

        var rolls = table.Mode switch
        {
            TombDropRollMode.IndependentChance => RollIndependentChance(table, rng, luck),
            TombDropRollMode.WeightedWithoutReplacement => RollWeighted(table, rng),
            _ => Array.Empty<TombDropRoll>(),
        };

        var items = new List<string>(capacity: rolls.Length);
        foreach (var r in rolls)
        {
            if (string.IsNullOrWhiteSpace(r.ItemId) || r.Quantity <= 0)
                continue;

            // Expand quantity into repeated entries for compatibility.
            for (var i = 0; i < r.Quantity; i++)
                items.Add(r.ItemId);
        }

        return items;
    }

    /// <summary>Create a luck context from the current player.</summary>
    public static TombLuckContext FromPlayer()
        => new(Game1.player.DailyLuck, Game1.player.team.AverageDailyLuck());

    private static TombDropRoll[] RollIndependentChance(
        TombDropTableDefinition table,
        Random rng,
        TombLuckContext luck
    )
    {
        var entries = new List<TombDropEntry>(table.Entries);

        // Shuffle for fairness (mirrors prior behavior).
        for (var i = entries.Count - 1; i > 0; i--)
        {
            var j = rng.Next(0, i + 1);
            (entries[i], entries[j]) = (entries[j], entries[i]);
        }

        var output = new List<TombDropRoll>(capacity: Math.Min(table.MaxItems, entries.Count));
        foreach (var e in entries)
        {
            if (output.Count >= table.MaxItems)
                break;

            if (string.IsNullOrWhiteSpace(e.ItemId))
                continue;

            var chance = e.Chance ?? 0;
            if (chance <= 0)
                continue;

            var adjusted = table.LuckyBonusEnabled ? ApplyLuckyBonus(chance, luck) : chance;
            if (rng.NextDouble() >= adjusted)
                continue;

            var qty = RollQuantity(e, rng);
            output.Add(new TombDropRoll(e.ItemId, qty));
        }

        return output.ToArray();
    }

    private static TombDropRoll[] RollWeighted(TombDropTableDefinition table, Random rng)
    {
        var candidates = table.Entries
            .Where(e => !string.IsNullOrWhiteSpace(e.ItemId) && (e.Weight ?? 0) > 0)
            .ToList();

        if (candidates.Count == 0)
            return Array.Empty<TombDropRoll>();

        var picks = Math.Min(table.MaxItems, candidates.Count);
        var output = new List<TombDropRoll>(capacity: picks);

        for (var p = 0; p < picks; p++)
        {
            var total = candidates.Sum(e => e.Weight ?? 0);
            if (total <= 0)
                break;

            var roll = rng.NextDouble() * total;
            var acc = 0.0;
            var idx = -1;

            for (var i = 0; i < candidates.Count; i++)
            {
                acc += candidates[i].Weight ?? 0;
                if (roll <= acc)
                {
                    idx = i;
                    break;
                }
            }

            if (idx < 0)
                idx = candidates.Count - 1;

            var chosen = candidates[idx];
            candidates.RemoveAt(idx);

            var qty = RollQuantity(chosen, rng);
            output.Add(new TombDropRoll(chosen.ItemId, qty));
        }

        return output.ToArray();
    }

    private static int RollQuantity(TombDropEntry entry, Random rng)
    {
        var min = Math.Max(1, entry.MinQuantity);
        var max = Math.Max(min, entry.MaxQuantity);
        return rng.Next(min, max + 1);
    }

    private static double ApplyLuckyBonus(double originalValue, TombLuckContext luck)
    {
        var currentLuck = GetLuckyType(luck.DailyLuck);

        Func<double, double> luckyBonus = currentLuck switch
        {
            LuckyTypes.SuperUnlucky => value => value * 0,
            LuckyTypes.Unlucky => value => value - 0.001,
            LuckyTypes.Neutral => value => value,
            LuckyTypes.Lucky => value => value + 0.001,
            LuckyTypes.SuperLucky => value => value + 0.01,
            _ => value => value * 0,
        };

        var luckyBonus2 = originalValue * (1 + luck.TeamAverageDailyLuck);
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
