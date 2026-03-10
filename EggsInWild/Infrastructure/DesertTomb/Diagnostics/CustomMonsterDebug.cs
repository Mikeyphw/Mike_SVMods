using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using EggsInWild.Infrastructure.Config;

namespace EggsInWild.Infrastructure.DesertTomb.Diagnostics;

internal static class CustomMonsterDebug
{
#if WEGG_DEV
    private static Func<ModConfig>? _getConfig;
    private static IMonitor? _monitor;
    private static readonly Dictionary<string, long> _nextHeartbeatTicksByKey = new(StringComparer.Ordinal);

    internal static void Initialize(Func<ModConfig> getConfig, IMonitor monitor)
    {
        _getConfig = getConfig ?? throw new ArgumentNullException(nameof(getConfig));
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    }

    internal static bool MatangoEnabled => _getConfig?.Invoke().DebugMonsterMatangoLogging == true;
    internal static bool PharaohEnabled => _getConfig?.Invoke().DebugMonsterPharaohLogging == true;
    internal static bool CrawlerEnabled => _getConfig?.Invoke().DebugMonsterCrawlerLogging == true;
    internal static bool HurricaneEnabled => _getConfig?.Invoke().DebugMonsterHurricaneLogging == true;

    internal static void Matango(Monster monster, string message)
        => Log(MatangoEnabled, "Matango", monster, message);
    internal static void MatangoHeartbeat(Monster monster, string key, string message, uint intervalTicks = 60)
        => LogHeartbeat(MatangoEnabled, "Matango", monster, key, message, intervalTicks);

    internal static void Pharaoh(Monster monster, string message)
        => Log(PharaohEnabled, "Pharaoh", monster, message);
    internal static void PharaohHeartbeat(Monster monster, string key, string message, uint intervalTicks = 60)
        => LogHeartbeat(PharaohEnabled, "Pharaoh", monster, key, message, intervalTicks);

    internal static void Crawler(Monster monster, string message)
        => Log(CrawlerEnabled, "Crawler", monster, message);
    internal static void CrawlerHeartbeat(Monster monster, string key, string message, uint intervalTicks = 60)
        => LogHeartbeat(CrawlerEnabled, "Crawler", monster, key, message, intervalTicks);

    internal static void Hurricane(Monster monster, string message)
        => Log(HurricaneEnabled, "Hurricane", monster, message);
    internal static void HurricaneHeartbeat(Monster monster, string key, string message, uint intervalTicks = 60)
        => LogHeartbeat(HurricaneEnabled, "Hurricane", monster, key, message, intervalTicks);

    private static void Log(bool enabled, string tag, Monster monster, string message)
    {
        if (!enabled || _monitor is null)
            return;

        var tile = monster.TilePoint;
        _monitor.Log(
            $"[CustomMonsterDebug][{tag}] {message} pos=({monster.Position.X:0.##},{monster.Position.Y:0.##}) tile=({tile.X},{tile.Y}) hp={monster.Health}/{monster.MaxHealth} stateFacing={monster.FacingDirection}",
            LogLevel.Info);
    }

    private static void LogHeartbeat(bool enabled, string tag, Monster monster, string key, string message, uint intervalTicks)
    {
        if (!enabled)
            return;

        var fullKey = $"{tag}:{monster.GetHashCode()}:{key}";
        var now = Game1.ticks;
        if (_nextHeartbeatTicksByKey.TryGetValue(fullKey, out var nextTick) && now < nextTick)
            return;

        _nextHeartbeatTicksByKey[fullKey] = now + Math.Max(1L, intervalTicks);
        Log(true, tag, monster, message);
    }
#else
    internal static void Initialize(Func<ModConfig> getConfig, IMonitor monitor)
    {
    }

    internal static bool MatangoEnabled => false;
    internal static bool PharaohEnabled => false;
    internal static bool CrawlerEnabled => false;
    internal static bool HurricaneEnabled => false;

    internal static void Matango(Monster monster, string message)
    {
    }
    internal static void MatangoHeartbeat(Monster monster, string key, string message, uint intervalTicks = 60)
    {
    }

    internal static void Pharaoh(Monster monster, string message)
    {
    }
    internal static void PharaohHeartbeat(Monster monster, string key, string message, uint intervalTicks = 60)
    {
    }

    internal static void Crawler(Monster monster, string message)
    {
    }
    internal static void CrawlerHeartbeat(Monster monster, string key, string message, uint intervalTicks = 60)
    {
    }

    internal static void Hurricane(Monster monster, string message)
    {
    }
    internal static void HurricaneHeartbeat(Monster monster, string key, string message, uint intervalTicks = 60)
    {
    }
#endif
}
