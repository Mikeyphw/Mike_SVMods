using System;
using System.Collections.Generic;
using WildEggsHatching.DesertTomb.Core.Model.Loot;

namespace WildEggsHatching.Infrastructure.DesertTomb.Loot.Definitions;

public sealed class TombDropTableDefinitionJson
{
    public string? Mode { get; set; }
    public int? MaxItems { get; set; }
    public bool? LuckyBonusEnabled { get; set; }
    public List<TombDropEntryJson>? Entries { get; set; }

    public TombDropTableDefinition ToModel(string id)
    {
        var mode = Enum.TryParse<TombDropRollMode>(Mode ?? string.Empty, ignoreCase: true, out var parsed)
            ? parsed
            : TombDropRollMode.IndependentChance;

        var max = Math.Max(0, MaxItems ?? 0);
        var lucky = LuckyBonusEnabled ?? false;

        var entries = new List<TombDropEntry>();
        if (Entries is not null)
        {
            foreach (var e in Entries)
            {
                if (e is null)
                    continue;

                var item = (e.Item ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(item))
                    continue;

                var minQty = Math.Max(1, e.MinQuantity ?? 1);
                var maxQty = Math.Max(minQty, e.MaxQuantity ?? minQty);

                double? chance = e.Chance;
                double? weight = e.Weight;

                // Allow either chance or weight depending on table mode.
                if (chance is null && weight is null)
                    chance = 0;

                entries.Add(new TombDropEntry(item, chance, weight, minQty, maxQty));
            }
        }

        return new TombDropTableDefinition(
            DropTableId: id,
            Mode: mode,
            MaxItems: max,
            LuckyBonusEnabled: lucky,
            Entries: entries
        );
    }
}

public sealed class TombDropEntryJson
{
    public string? Item { get; set; }
    public double? Chance { get; set; }
    public double? Weight { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
}
