using System;
using Microsoft.Xna.Framework;

namespace WildEggsHatching.Application.DesertTomb.Spawning;

/// <summary>
/// ModData keys and parsing helpers for tomb-spawned monster behavior metadata (Phase 4).
/// </summary>
internal static class TombMonsterBehaviorModData
{
    public const string MonsterTypeKey = Constants.ModId + "/deserttomb/type";
    public const string SpawnAreaIdKey = Constants.ModId + "/deserttomb/areaId";

    public const string RoleKey = Constants.ModId + "/deserttomb/role";
    public const string HomeTileKey = Constants.ModId + "/deserttomb/homeTile";
    public const string LeashRadiusKey = Constants.ModId + "/deserttomb/leashRadius";
    public const string LeashRectKey = Constants.ModId + "/deserttomb/leashRect";
    public const string LeashTicksKey = Constants.ModId + "/deserttomb/leashTicks";

    public static string FormatPoint(Point p) => $"{p.X},{p.Y}";

    public static bool TryParsePoint(string? value, out Point point)
    {
        point = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Split(',');
        if (parts.Length != 2)
            return false;

        return int.TryParse(parts[0], out point.X) && int.TryParse(parts[1], out point.Y);
    }

    public static string FormatRect(Rectangle r) => $"{r.X},{r.Y},{r.Width},{r.Height}";

    public static bool TryParseRect(string? value, out Rectangle rect)
    {
        rect = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Split(',');
        if (parts.Length != 4)
            return false;

        if (!int.TryParse(parts[0], out var x))
            return false;
        if (!int.TryParse(parts[1], out var y))
            return false;
        if (!int.TryParse(parts[2], out var w))
            return false;
        if (!int.TryParse(parts[3], out var h))
            return false;

        rect = new Rectangle(x, y, w, h);
        return true;
    }

    public static int ParseIntOrDefault(string? value, int defaultValue)
        => int.TryParse(value, out var v) ? v : defaultValue;

    public static void WriteInt(ref string? destination, int value)
        => destination = value.ToString();
}
