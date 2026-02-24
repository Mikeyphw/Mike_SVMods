using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Pathfinding;
using EggsInWild.DesertTomb.Core.Spawning;
using EggsInWild.Infrastructure.DesertTomb.Monsters.Creatures;
using Mikew.NestCore.Helpers.Logging;

namespace EggsInWild.Application.DesertTomb.Spawning;

/// <summary>
/// Lightweight behavior enforcer for role metadata stored in <see cref="Monster.modData"/>.
/// </summary>
public sealed class TombRoleEnforcer
{
    private const string RoleKey = Constants.ModId + "/deserttomb/role";
    private const string HomeTileKey = Constants.ModId + "/deserttomb/homeTile";
    private const string LeashRadiusKey = Constants.ModId + "/deserttomb/leashRadius";
    private const string LeashRectKey = Constants.ModId + "/deserttomb/leashRect";
    private const string PatrolPointsKey = Constants.ModId + "/deserttomb/patrolPoints";
    private const string PatrolIndexKey = Constants.ModId + "/deserttomb/patrolIndex";

    private static readonly FieldInfo? ControllerField = typeof(Character).GetField(
        "controller",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
    );

    private readonly ILog _log;
    private readonly int _intervalTicks;
    private readonly int _aggroRangeTiles;

    private int _ticks;

    public TombRoleEnforcer(ILog log, int intervalTicks = 10, int aggroRangeTiles = 7)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _intervalTicks = Math.Max(1, intervalTicks);
        _aggroRangeTiles = Math.Max(1, aggroRangeTiles);
    }

    public void UpdateTicked(MineShaft mineShaft)
    {
        if (mineShaft is null)
            throw new ArgumentNullException(nameof(mineShaft));

        _ticks++;
        if (_ticks % _intervalTicks != 0)
            return;

        var player = Game1.player;
        if (player is null)
            return;

        var playerTile = player.TilePoint;

        for (int i = 0; i < mineShaft.characters.Count; i++)
        {
            if (mineShaft.characters[i] is not Monster monster)
                continue;

            // Pharaoh has its own leash/stuck logic.
            if (monster is ThePharaoh)
                continue;

            if (!monster.modData.TryGetValue(RoleKey, out var roleStr))
                continue;

            if (!Enum.TryParse(roleStr, out TombMonsterRole role))
                continue;

            if (!TryParsePoint(monster.modData.TryGetValue(HomeTileKey, out var homeStr) ? homeStr : null, out var homeTile))
                continue;

            var tile = monster.TilePoint;

            if (TryParseRect(monster.modData.TryGetValue(LeashRectKey, out var rectStr) ? rectStr : null, out var leashRect)
                && !leashRect.Contains(tile))
            {
                TeleportHome(monster, homeTile);
                continue;
            }

            if (TryParseInt(monster.modData.TryGetValue(LeashRadiusKey, out var radiusStr) ? radiusStr : null, out var leashRadius)
                && leashRadius > 0)
            {
                var dx = tile.X - homeTile.X;
                var dy = tile.Y - homeTile.Y;
                if ((dx * dx + dy * dy) > (leashRadius * leashRadius))
                {
                    TeleportHome(monster, homeTile);
                    continue;
                }
            }

            if (role != TombMonsterRole.Patrol)
                continue;

            // Don't fight the monster AI when the player is nearby.
            var pdx = tile.X - playerTile.X;
            var pdy = tile.Y - playerTile.Y;
            if ((pdx * pdx + pdy * pdy) <= (_aggroRangeTiles * _aggroRangeTiles))
            {
                ClearController(monster);
                continue;
            }

            if (!monster.modData.TryGetValue(PatrolPointsKey, out var pointsStr))
                continue;

            var points = ParsePoints(pointsStr);
            if (points.Count < 2)
                continue;

            var index = 0;
            if (monster.modData.TryGetValue(PatrolIndexKey, out var idxStr))
                _ = int.TryParse(idxStr, out index);

            index = ((index % points.Count) + points.Count) % points.Count;

            var target = points[index];
            if (tile == target)
            {
                index = (index + 1) % points.Count;
                target = points[index];
                monster.modData[PatrolIndexKey] = index.ToString();
            }

            TrySetController(monster, mineShaft, target);
        }
    }

    private void TrySetController(Monster monster, MineShaft location, Point targetTile)
    {
        try
        {
            var controller = new PathFindController(monster, location, targetTile, -1, null, 300);
            if (controller?.pathToEndPoint is null || controller.pathToEndPoint.Count == 0)
                return;

            ControllerField?.SetValue(monster, controller);
        }
        catch (Exception ex)
        {
            _log.Log($"[DesertTomb] Patrol controller failed: {ex.Message}", LogLevel.Trace);
        }
    }

    private static void ClearController(Monster monster)
        => ControllerField?.SetValue(monster, null);

    private static void TeleportHome(Monster monster, Point homeTile)
    {
        ClearController(monster);
        monster.Halt();
        monster.Position = new Vector2(homeTile.X, homeTile.Y) * 64f;
    }

    private static bool TryParsePoint(string? value, out Point point)
    {
        point = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Split(',');
        if (parts.Length != 2)
            return false;

        return int.TryParse(parts[0], out point.X) && int.TryParse(parts[1], out point.Y);
    }

    private static bool TryParseInt(string? value, out int parsed)
        => int.TryParse(value, out parsed);

    private static bool TryParseRect(string? value, out Rectangle rect)
    {
        rect = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Split(',');
        if (parts.Length != 4)
            return false;

        return int.TryParse(parts[0], out rect.X)
            && int.TryParse(parts[1], out rect.Y)
            && int.TryParse(parts[2], out rect.Width)
            && int.TryParse(parts[3], out rect.Height);
    }

    private static List<Point> ParsePoints(string value)
    {
        var list = new List<Point>();
        if (string.IsNullOrWhiteSpace(value))
            return list;

        var items = value.Split(';');
        foreach (var item in items)
        {
            if (TryParsePoint(item, out var p))
                list.Add(p);
        }

        return list;
    }
}
