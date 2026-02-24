using System;
using System.Collections.Generic;
using System.Globalization;
using Mikew.NestCore.Framework.Random;
using RoosterControl.Enums;

namespace RoosterControl.Application.FarmAnimals;

/// <summary>
/// Centralizes weighted selection and percent normalization for FarmAnimals-related rules.
/// </summary>
internal sealed class FarmAnimalRatioWeightingService
{
    public Dictionary<RoosterItemDataEntries, string> RollChosenProduce(
        IReadOnlyDictionary<ProduceEntries, Dictionary<RoosterItemDataEntries, string>> entries,
        IRandomProvider rng
    )
    {
        if (entries is null) throw new ArgumentNullException(nameof(entries));
        if (rng is null) throw new ArgumentNullException(nameof(rng));

        var table = new WeightedTable<Dictionary<RoosterItemDataEntries, string>>(rng);

        foreach (var (key, val) in entries)
        {
            if (key == ProduceEntries.Chosen)
                continue;

            if (val is null)
                continue;

            _ = val.TryGetValue(RoosterItemDataEntries.Probability, out var raw);
            _ = int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var weight);

            table.Add(val, weight);
        }

        return table.RollOrDefault(new Dictionary<RoosterItemDataEntries, string>());
    }

    public static int NormalizePercent(int percent) => Math.Clamp(percent, 0, 100);

    public static float NormalizePercent(float percent)
    {
        if (float.IsNaN(percent) || float.IsInfinity(percent))
            return 0;

        if (percent <= 0)
            return 0;

        if (percent >= 100)
            return 100;

        return percent;
    }
}
