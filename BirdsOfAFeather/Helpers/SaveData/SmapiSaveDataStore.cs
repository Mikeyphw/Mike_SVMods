using System;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using StardewModdingAPI;

namespace Mikew.BirdsOfAFeather.Helpers.SaveData
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
            try
            {
                return _helper.Data.ReadSaveData<T>(key);
            }
            catch (Exception ex)
            {
                _log.Log($"Failed to read save data '{key}'.\n{ex}", LogLevel.Error);
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
            try
            {
                _helper.Data.WriteSaveData(key, data);
            }
            catch (Exception ex)
            {
                _log.Log($"Failed to write save data '{key}'.\n{ex}", LogLevel.Error);
            }
        }

        public void Delete(string key)
        {
            try
            {
                _helper.Data.WriteSaveData<object?>(key, null);
            }
            catch (Exception ex)
            {
                _log.Log($"Failed to delete save data '{key}'.\n{ex}", LogLevel.Error);
            }
        }
    }
}
