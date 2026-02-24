using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace EggsInWild.Application.Spawning;

public static class RectParsing
{
    public static IReadOnlyList<Rectangle> ParseMany(IEnumerable<string> raw, Action<string>? onError = null)
    {
        var list = new List<Rectangle>();
        foreach (var s in raw)
        {
            if (TryParse(s, out var rect, onError))
                list.Add(rect);
        }
        return list;
    }

    public static bool TryParse(string raw, out Rectangle rect, Action<string>? onError = null)
    {
        rect = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        // Expected: "x1,y1;x2,y2"
        var parts = raw.Split(';');
        if (parts.Length != 2)
        {
            onError?.Invoke($"Invalid rectangle '{raw}': expected 'x1,y1;x2,y2'.");
            return false;
        }

        if (!TryParsePoint(parts[0], out var x1, out var y1) || !TryParsePoint(parts[1], out var x2, out var y2))
        {
            onError?.Invoke($"Invalid rectangle '{raw}': invalid point.");
            return false;
        }

        var left = Math.Min(x1, x2);
        var top = Math.Min(y1, y2);
        var right = Math.Max(x1, x2);
        var bottom = Math.Max(y1, y2);

        // Inclusive bounds => +1
        rect = new Rectangle(left, top, right - left + 1, bottom - top + 1);
        return true;
    }

    private static bool TryParsePoint(string raw, out int x, out int y)
    {
        x = 0;
        y = 0;
        var parts = raw.Split(',');
        if (parts.Length != 2)
            return false;
        return int.TryParse(parts[0].Trim(), out x) && int.TryParse(parts[1].Trim(), out y);
    }

    public static bool ContainsAny(IReadOnlyList<Rectangle> rects, int x, int y)
    {
        for (var i = 0; i < rects.Count; i++)
        {
            if (rects[i].Contains(x, y))
                return true;
        }
        return false;
    }
}
