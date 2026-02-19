using System;
using System.Collections.Generic;
using RoosterAddon.Enums;
using StardewValley.GameData.Objects;

namespace RoosterAddon.Application.Objects;

/// <summary>Applies Better Truffles context tagging for rooster-produced items.</summary>
internal sealed class BetterTrufflesContextTagService
{
    private const string BetterTrufflesTag = "temisthem_bettertruffles";

    public void Apply(IDictionary<string, ObjectData> objects, IEnumerable<RoosterData> roosters)
    {
        if (objects is null)
            throw new ArgumentNullException(nameof(objects));
        if (roosters is null)
            return;

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rooster in roosters)
        {
            if (rooster?.AllProduces is null)
                continue;

            TryAddProduceId(ids, rooster, ProduceTypeEntries.Produce);
            TryAddProduceId(ids, rooster, ProduceTypeEntries.Deluxe);
        }

        if (ids.Count == 0)
            return;

        foreach (var (id, data) in objects)
        {
            if (!ids.Contains(id))
                continue;

            data.ContextTags ??= new();
            if (!data.ContextTags.Contains(BetterTrufflesTag))
                data.ContextTags.Add(BetterTrufflesTag);
        }
    }

    private static void TryAddProduceId(HashSet<string> ids, RoosterData rooster, ProduceTypeEntries type)
    {
        if (!rooster.AllProduces.TryGetValue(type, out var bySlot))
            return;

        if (!bySlot.TryGetValue(ProduceEntries.First, out var first))
            return;

        if (!first.TryGetValue(RoosterItemDataEntries.Name, out var id))
            return;

        if (!string.IsNullOrWhiteSpace(id))
            ids.Add(id);
    }
}
