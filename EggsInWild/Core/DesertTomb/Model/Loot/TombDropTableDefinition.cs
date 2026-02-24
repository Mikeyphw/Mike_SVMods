using System;
using System.Collections.Generic;

namespace EggsInWild.DesertTomb.Core.Model.Loot;

/// <summary>
/// Data-driven drop table for DesertTomb monsters.
/// The engine consumes these definitions and produces qualified item IDs to be dropped.
/// </summary>
public sealed record TombDropTableDefinition(
    string DropTableId,
    TombDropRollMode Mode,
    int MaxItems,
    bool LuckyBonusEnabled,
    IReadOnlyList<TombDropEntry> Entries
)
{
    public static TombDropTableDefinition Empty(string id)
        => new(
            DropTableId: id,
            Mode: TombDropRollMode.IndependentChance,
            MaxItems: 0,
            LuckyBonusEnabled: false,
            Entries: Array.Empty<TombDropEntry>()
        );
}

/// <summary>How a drop table is rolled.</summary>
public enum TombDropRollMode
{
    /// <summary>
    /// Each entry is rolled independently using <see cref="TombDropEntry.Chance"/>.
    /// Entries are evaluated in RNG-shuffled order. The result is capped at <see cref="TombDropTableDefinition.MaxItems"/>.
    /// </summary>
    IndependentChance = 0,

    /// <summary>
    /// Select up to <see cref="TombDropTableDefinition.MaxItems"/> distinct entries using <see cref="TombDropEntry.Weight"/>.
    /// </summary>
    WeightedWithoutReplacement = 1,
}

/// <summary>
/// A single drop entry.
/// For <see cref="TombDropRollMode.IndependentChance"/>, use <see cref="Chance"/>.
/// For <see cref="TombDropRollMode.WeightedWithoutReplacement"/>, use <see cref="Weight"/>.
/// </summary>
public sealed record TombDropEntry(
    string ItemId,
    double? Chance,
    double? Weight,
    int MinQuantity,
    int MaxQuantity
)
{
    public static TombDropEntry ChanceEntry(string itemId, double chance, int minQty = 1, int maxQty = 1)
        => new(itemId, Chance: chance, Weight: null, MinQuantity: minQty, MaxQuantity: maxQty);

    public static TombDropEntry WeightEntry(string itemId, double weight, int minQty = 1, int maxQty = 1)
        => new(itemId, Chance: null, Weight: weight, MinQuantity: minQty, MaxQuantity: maxQty);
}

/// <summary>Luck inputs used to apply prior-style lucky bonus adjustments.</summary>
public sealed record TombLuckContext(double DailyLuck, double TeamAverageDailyLuck)
{
    public static TombLuckContext Neutral => new(0, 0);
}

/// <summary>Result of rolling a drop entry.</summary>
public sealed record TombDropRoll(string ItemId, int Quantity);
