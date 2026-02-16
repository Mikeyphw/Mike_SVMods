using System;
using Microsoft.Xna.Framework;
using StardewValley.Locations;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Core.Spawning;
using WildEggsHatching.Infrastructure.Logging;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.Application.DesertTomb.Spawning;

/// <summary>
/// Phase 4: lightweight, data-driven role enforcement using monster.modData.
/// This keeps spawned monsters from being lured too far from their intended area.
/// </summary>
internal sealed class TombRoleEnforcer
{
    private readonly ILog _log;
    private readonly int _intervalTicks;
    private readonly int _guardTeleportTicks;
    private readonly int _wanderTeleportTicks;

    private int _ticks;

    public TombRoleEnforcer(
        ILog log,
        int intervalTicks = 10,
        int guardTeleportTicks = 60,
        int wanderTeleportTicks = 120
    )
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _intervalTicks = Math.Max(1, intervalTicks);
        _guardTeleportTicks = Math.Max(_intervalTicks, guardTeleportTicks);
        _wanderTeleportTicks = Math.Max(_intervalTicks, wanderTeleportTicks);
    }

    public void UpdateTicked(MineShaft mineShaft)
    {
        _ticks++;

        if (_ticks % _intervalTicks != 0)
            return;

        foreach (var c in mineShaft.characters)
        {
            if (c is not Monster m)
                continue;

            if (!m.modData.TryGetValue(TombMonsterBehaviorModData.RoleKey, out var roleStr))
                continue;

            if (!Enum.TryParse(roleStr, ignoreCase: true, out MonsterSpawnRole role))
                continue;

            // Pharaoh has its own leash/stuck logic; don't fight it.
            if (m.modData.TryGetValue(TombMonsterBehaviorModData.MonsterTypeKey, out var typeStr)
                && string.Equals(typeStr, TombMonsterType.TheTombPharaoh.ToString(), StringComparison.Ordinal))
            {
                continue;
            }

            if (!m.modData.TryGetValue(TombMonsterBehaviorModData.HomeTileKey, out var homeStr)
                || !TombMonsterBehaviorModData.TryParsePoint(homeStr, out var home))
            {
                continue;
            }

            var leashRadius = TombMonsterBehaviorModData.ParseIntOrDefault(
                m.modData.TryGetValue(TombMonsterBehaviorModData.LeashRadiusKey, out var leashStr) ? leashStr : null,
                defaultValue: 12
            );

            var hasRect = m.modData.TryGetValue(TombMonsterBehaviorModData.LeashRectKey, out var rectStr)
                && TombMonsterBehaviorModData.TryParseRect(rectStr, out var leashRect);

            var tile = m.getTileLocationPoint();

            var outside = hasRect
                ? !leashRect.Contains(tile)
                : DistanceSquared(tile, home) > leashRadius * leashRadius;

            if (!outside)
            {
                m.modData.Remove(TombMonsterBehaviorModData.LeashTicksKey);
                continue;
            }

            var leashTicks = TombMonsterBehaviorModData.ParseIntOrDefault(
                m.modData.TryGetValue(TombMonsterBehaviorModData.LeashTicksKey, out var ticksStr) ? ticksStr : null,
                defaultValue: 0
            );

            leashTicks += _intervalTicks;
            m.modData[TombMonsterBehaviorModData.LeashTicksKey] = leashTicks.ToString();

            var threshold = role == MonsterSpawnRole.Guard ? _guardTeleportTicks : _wanderTeleportTicks;
            if (leashTicks < threshold)
                continue;

            var target = hasRect ? ClampToRect(home, leashRect) : home;

            try
            {
                m.Halt();
                m.Position = new Vector2(target.X, target.Y) * 64f;
            }
            catch (Exception ex)
            {
                _log.Error($"[DesertTomb] Leash teleport failed for {m.Name}: {ex}");
            }

            m.modData.Remove(TombMonsterBehaviorModData.LeashTicksKey);
        }
    }

    private static int DistanceSquared(Point a, Point b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    private static Point ClampToRect(Point p, Rectangle rect)
    {
        var x = Math.Clamp(p.X, rect.Left, rect.Right - 1);
        var y = Math.Clamp(p.Y, rect.Top, rect.Bottom - 1);
        return new Point(x, y);
    }
}
