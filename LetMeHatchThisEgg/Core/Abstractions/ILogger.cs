using StardewModdingAPI;

namespace LetMeHatchThisEgg.Core.Abstractions;

/// <summary>Minimal logging abstraction used by LMH services.</summary>
public interface ILogger
{
    /// <summary>Logs a message at the given level.</summary>
    void Log(LogLevel level, string message, Exception? exception = null);

    /// <summary>Creates a logger scoped to a category/prefix.</summary>
    ILogger ForCategory(string category);
}
