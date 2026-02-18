using System;
using StardewModdingAPI;

namespace Mikew.BirdsOfAFeather.Helpers.Logging
{
    /// <summary>
    /// Minimal logging abstraction used across mods to avoid repeated wrappers.
    /// </summary>
    public interface ILog
    {
        void Log(string message, LogLevel level = LogLevel.Trace, bool enabled = true);
        void VerboseLog(string message, LogLevel level = LogLevel.Debug, bool enabled = true);
        void TestingLog(string message, LogLevel level = LogLevel.Debug, bool enabled = true);
        void LogException(Exception exception, string message, LogLevel level = LogLevel.Error, bool enabled = true);
    }
}
