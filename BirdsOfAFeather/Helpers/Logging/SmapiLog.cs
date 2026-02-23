using System;
using Mikew.BirdsOfAFeather.Framework;
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
        private readonly Func<CoreModConfig?>? _configProvider;
        private readonly string _defaultMod;
        private readonly string _defaultCategory;

        public SmapiLog(
            IMonitor monitor,
            Func<bool>? verboseEnabled = null,
            Func<bool>? testingEnabled = null,
            Func<CoreModConfig?>? configProvider = null,
            string defaultMod = ScopedLogCatalog.ModBoaf,
            string defaultCategory = ScopedLogCatalog.CategoryGeneral)
        {
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _verboseEnabled = verboseEnabled;
            _testingEnabled = testingEnabled;
            _configProvider = configProvider;
            _defaultMod = ScopedLogCatalog.NormalizeModTag(defaultMod);
            _defaultCategory = ScopedLogCatalog.NormalizeCategory(defaultCategory);
        }

        public void Log(string message, LogLevel level = LogLevel.Trace, bool enabled = true)
        {
            LogScoped(_defaultMod, _defaultCategory, message, level, enabled);
        }

        public void VerboseLog(string message, LogLevel level = LogLevel.Debug, bool enabled = true)
        {
            if (!enabled)
                return;

            if (_verboseEnabled != null && !_verboseEnabled())
                return;

            LogScoped(_defaultMod, _defaultCategory, message, level);
        }

        public void TestingLog(string message, LogLevel level = LogLevel.Debug, bool enabled = true)
        {
            if (!enabled)
                return;

            if (_testingEnabled != null && !_testingEnabled())
                return;

            LogScoped(_defaultMod, _defaultCategory, message, level);
        }

        public void LogException(Exception exception, string message, LogLevel level = LogLevel.Error, bool enabled = true)
        {
            LogScoped(_defaultMod, _defaultCategory, $"{message}\n{exception}", level, enabled);
        }

        public void LogScoped(
            string mod,
            string category,
            string message,
            LogLevel level = LogLevel.Trace,
            bool enabled = true)
        {
            if (!enabled)
                return;

            var modTag = ScopedLogCatalog.NormalizeModTag(mod);
            var categoryTag = ScopedLogCatalog.NormalizeCategory(category);

            if (!ShouldEmit(modTag, categoryTag, level))
                return;

            var levelTag = ScopedLogCatalog.ToLevelToken(level);
            _monitor.Log($"[{modTag}][{categoryTag}][{levelTag}] {message}", level);
        }

        private bool ShouldEmit(string mod, string category, LogLevel level)
        {
            if (_configProvider is null)
                return true;

            var cfg = _configProvider();
            if (cfg is null)
                return true;

            cfg.EnsureScopedLoggingDefaults();

            var modConfig = cfg.ScopedLogging.GetByMod(mod);
            var effective = modConfig.ResolveEffectiveLevel(category);
            return ScopedLogCatalog.ShouldEmit(effective, level);
        }
    }
}
