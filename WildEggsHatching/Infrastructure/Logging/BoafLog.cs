using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;
using WildEggsHatching.Infrastructure.Config;

namespace WildEggsHatching.Infrastructure.Logging;

/// <summary>Thin adapter that routes this mod's logs through BirdsOfAFeather's logger.</summary>
public sealed class BoafLog : ILog
{
    private readonly Logger _logger;

    public BoafLog(IMonitor monitor, ModConfig config)
    {
        var boaf = new CoreModConfig
        {
            VerboseLogging = config.DesertTombDebugLogging || config.AnimalNotesDebugMode,
            TestingLogging = config.AnimalNotesDebugMode,
        };

        _logger = new Logger(monitor, boaf);
    }

    public Logger Inner => _logger;

    public void Log(string message, LogLevel level = LogLevel.Debug)
        => _logger.Log(message, level);
}
