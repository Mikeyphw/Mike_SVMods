using System;
using StardewModdingAPI;

namespace Mikew.BirdsOfAFeather.Helpers.Logging
{
    /// <summary>
    /// Simple SMAPI-backed logger which optionally supports verbose/testing gates.
    /// </summary>
    public sealed class SmapiLog : ILog
    {
        private readonly IMonitor _monitor;
        private readonly Func<bool>? _verboseEnabled;
        private readonly Func<bool>? _testingEnabled;

        public SmapiLog(IMonitor monitor, Func<bool>? verboseEnabled = null, Func<bool>? testingEnabled = null)
        {
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _verboseEnabled = verboseEnabled;
            _testingEnabled = testingEnabled;
        }

        public void Log(string message, LogLevel level = LogLevel.Trace, bool enabled = true)
        {
            if (!enabled)
                return;

            _monitor.Log(message, level);
        }

        public void VerboseLog(string message, LogLevel level = LogLevel.Debug, bool enabled = true)
        {
            if (!enabled)
                return;

            if (_verboseEnabled != null && !_verboseEnabled())
                return;

            _monitor.Log(message, level);
        }

        public void TestingLog(string message, LogLevel level = LogLevel.Debug, bool enabled = true)
        {
            if (!enabled)
                return;

            if (_testingEnabled != null && !_testingEnabled())
                return;

            _monitor.Log(message, level);
        }

        public void LogException(Exception exception, string message, LogLevel level = LogLevel.Error, bool enabled = true)
        {
            if (!enabled)
                return;

            _monitor.Log($"{message}\n{exception}", level);
        }
    }
}
