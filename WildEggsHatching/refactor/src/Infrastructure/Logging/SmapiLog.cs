using StardewModdingAPI;

namespace WildEggsHatching.Infrastructure.Logging;

public sealed class SmapiLog : ILog
{
    private readonly IMonitor _monitor;

    public SmapiLog(IMonitor monitor)
    {
        _monitor = monitor;
    }

    public void Log(string message, LogLevel level = LogLevel.Debug)
        => _monitor.Log(message, level);
}
