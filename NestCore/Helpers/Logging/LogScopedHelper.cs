using System;
using StardewModdingAPI;

namespace Mikew.NestCore.Helpers.Logging;

/// <summary>
/// Shared helper for emitting scoped log messages that works with any ILog implementation.
/// This eliminates the need to repeat the type-check pattern in every class.
/// </summary>
public static class LogScopedHelper
{
    /// <summary>
    /// Log a message with a scoped category, falling back to basic log if not a SmapiLog/Logger instance.
    /// </summary>
    public static void Log(ILog log, ScopedLogCatalog.ModWeCategory category, string message, LogLevel level = LogLevel.Trace)
    {
        if (log is SmapiLog slog)
            slog.LogScoped(category, message, level);
        else if (log is Logger logger)
            logger.LogScoped(category, message, level);
        else
            log.Log(message, level);
    }

    /// <summary>
    /// Log a message with a scoped category, falling back to basic log if not a SmapiLog/Logger instance.
    /// </summary>
    public static void Log(ILog log, ScopedLogCatalog.ModRoosterControlCategory category, string message, LogLevel level = LogLevel.Trace)
    {
        if (log is SmapiLog slog)
            slog.LogScoped(category, message, level);
        else if (log is Logger logger)
            logger.LogScoped(category, message, level);
        else
            log.Log(message, level);
    }

    /// <summary>
    /// Log a message with a scoped category, falling back to basic log if not a SmapiLog/Logger instance.
    /// </summary>
    public static void Log(ILog log, ScopedLogCatalog.ModLthteCategory category, string message, LogLevel level = LogLevel.Trace)
    {
        if (log is SmapiLog slog)
            slog.LogScoped(category, message, level);
        else if (log is Logger logger)
            logger.LogScoped(category, message, level);
        else
            log.Log(message, level);
    }

    /// <summary>
    /// Log a message with a scoped category, falling back to basic log if not a SmapiLog/Logger instance.
    /// </summary>
    public static void Log(ILog log, ScopedLogCatalog.ModNestCoreCategory category, string message, LogLevel level = LogLevel.Trace)
    {
        if (log is SmapiLog slog)
            slog.LogScoped(category, message, level);
        else if (log is Logger logger)
            logger.LogScoped(category, message, level);
        else
            log.Log(message, level);
    }
}

