using System;
using Mikew.NestCore.Helpers.Logging;
using StardewModdingAPI;

using LogScopedHelper = Mikew.NestCore.Helpers.Logging.LogScopedHelper;
using ScopedLogCatalog = Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace Mikew.NestCore.Helpers.SaveData
{
    /// <summary>
    /// SMAPI-backed implementation of <see cref="ISaveDataStore"/>.
    /// </summary>
    public sealed class SmapiSaveDataStore : ISaveDataStore
    {
        private readonly IModHelper _helper;
        private readonly ILog _log;

        public SmapiSaveDataStore(IModHelper helper, ILog log)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public SmapiSaveDataStore(IModHelper helper, IMonitor monitor)
            : this(helper, new SmapiLog(monitor))
        {
        }

        public T? Read<T>(string key) where T : class
        {
            var normalizedKey = NormalizeKey(key);
            try
            {
                return _helper.Data.ReadSaveData<T>(normalizedKey);
            }
            catch (Exception ex)
            {
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModNestCoreCategory.SaveData, $"Failed to read save data '{key}' (normalized: '{normalizedKey}').\n{ex}", LogLevel.Error);
                return null;
            }
        }

        public T ReadOrCreate<T>(string key, Func<T> factory) where T : class
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var existing = Read<T>(key);
            if (existing != null)
                return existing;

            var created = factory();
            Write(key, created);
            return created;
        }

        public void Write<T>(string key, T data) where T : class
        {
            var normalizedKey = NormalizeKey(key);
            try
            {
                _helper.Data.WriteSaveData(normalizedKey, data);
            }
            catch (Exception ex)
            {
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModNestCoreCategory.SaveData, $"Failed to write save data '{key}' (normalized: '{normalizedKey}').\n{ex}", LogLevel.Error);
            }
        }

        public void Delete(string key)
        {
            var normalizedKey = NormalizeKey(key);
            try
            {
                _helper.Data.WriteSaveData<object>(normalizedKey, null);
            }
            catch (Exception ex)
            {
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModNestCoreCategory.SaveData, $"Failed to delete save data '{key}' (normalized: '{normalizedKey}').\n{ex}", LogLevel.Error);
            }
        }

        private static string NormalizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return "_";

            var chars = key.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                if (char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '-')
                    continue;

                chars[i] = '.';
            }

            return new string(chars);
        }
    }
}

