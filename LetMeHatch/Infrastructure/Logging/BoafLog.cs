using System;
using Mikew.NestCore.Helpers;
using StardewModdingAPI;

namespace Mikew.NestCore.Helpers.Logging;

public sealed class NestCoreLog : ILog
{
    private readonly Logger _logger;

    public NestCoreLog(Logger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Log(string message, LogLevel level = LogLevel.Debug, bool enabled = true)
    {
        _logger.Log(message, level, enabled);
    }

    public void VerboseLog(string message, LogLevel level = LogLevel.Debug, bool enabled = true)
    {
        _logger.VerboseLog(message, level, enabled);
    }

    public void TestingLog(string message, LogLevel level = LogLevel.Debug, bool enabled = true)
    {
        _logger.TestingLog(message, level, enabled);
    }

    public void LogException(Exception exception, string message = "", LogLevel level = LogLevel.Error, bool enabled = true)
    {
        _logger.LogException(exception, message, level, enabled);
    }
}
