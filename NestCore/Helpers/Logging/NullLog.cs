using System;
using StardewModdingAPI;

namespace Mikew.NestCore.Helpers.Logging
{
    /// <summary>
    /// No-op logger for tests or optional wiring.
    /// </summary>
    public sealed class NullLog : ILog
    {
        public static readonly NullLog Instance = new();

        private NullLog() { }

        public void Log(string message, LogLevel level = LogLevel.Trace, bool enabled = true) { }
        public void VerboseLog(string message, LogLevel level = LogLevel.Debug, bool enabled = true) { }
        public void TestingLog(string message, LogLevel level = LogLevel.Debug, bool enabled = true) { }
        public void LogException(Exception exception, string message, LogLevel level = LogLevel.Error, bool enabled = true) { }
    }
}
