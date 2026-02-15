using StardewModdingAPI;

namespace WildEggsHatching.Infrastructure.Logging;

public interface ILog
{
    void Log(string message, LogLevel level = LogLevel.Debug);
}
