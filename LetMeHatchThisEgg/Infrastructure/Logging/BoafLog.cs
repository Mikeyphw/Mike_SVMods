using System;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;

namespace Mikew.BirdsOfAFeather.Helpers.Logging;

public sealed class BoafLog : ILog
{
    private readonly Logger _logger;

    public BoafLog(Logger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Log(string message, LogLevel level = LogLevel.Debug)
    {
        _logger.Log(message, level, enabled: true);
    }
}
