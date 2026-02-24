using System;
using System.Collections.Generic;
using System.Linq;

namespace EggsInWild.Core.Weighting;

internal static class MedianMath
{
    public static double MedianOrDefault(IReadOnlyList<double> values, double defaultValue)
    {
        if (values is null || values.Count == 0)
            return defaultValue;

        var ordered = values.Where(v => double.IsFinite(v)).OrderBy(v => v).ToArray();
        if (ordered.Length == 0)
            return defaultValue;

        var mid = ordered.Length / 2;
        return ordered.Length % 2 == 1
            ? ordered[mid]
            : (ordered[mid - 1] + ordered[mid]) / 2.0;
    }
}
