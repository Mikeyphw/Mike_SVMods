using System;
using Mikew.NestCore.Helpers.Logging;
using StardewModdingAPI;

namespace Mikew.NestCore.Helpers.Config
{
    /// <summary>
    /// Provides a stable way to read/update/write SMAPI config with optional change notifications.
    /// </summary>
    public sealed class SmapiConfigProvider<TConfig>
        where TConfig : class, new()
    {
        private readonly IModHelper _helper;
        private readonly ILog _log;
        private TConfig _config;

        public event EventHandler<TConfig>? ConfigUpdated;

        public SmapiConfigProvider(IModHelper helper, ILog log)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _config = Read();
        }

        public SmapiConfigProvider(IModHelper helper, IMonitor monitor)
            : this(helper, new SmapiLog(monitor))
        {
        }

        public TConfig Get() => _config;

        public TConfig Value => _config;

        public void ResetToDefault(bool save = true)
        {
            _config = new TConfig();

            if (save)
                Save();

            ConfigUpdated?.Invoke(this, _config);
        }

        public TConfig Reload()
        {
            _config = Read();
            return _config;
        }

        public void Save()
        {
            _helper.WriteConfig(_config);
        }

        public void Update(Action<TConfig> update, bool save = true)
        {
            if (update == null)
                throw new ArgumentNullException(nameof(update));

            update(_config);

            if (save)
                Save();

            ConfigUpdated?.Invoke(this, _config);
        }

        private TConfig Read()
        {
            try
            {
                return _helper.ReadConfig<TConfig>() ?? new TConfig();
            }
            catch (Exception ex)
            {
                _log.Log($"Failed to read config. Using defaults.\n{ex}", LogLevel.Error);
                return new TConfig();
            }
        }
    }
}
