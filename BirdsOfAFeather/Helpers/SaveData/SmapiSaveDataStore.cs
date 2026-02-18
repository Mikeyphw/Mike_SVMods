using System;
using StardewModdingAPI;

namespace Mikew.BirdsOfAFeather.Helpers.SaveData
{
    /// <summary>
    /// SMAPI-backed implementation of <see cref="ISaveDataStore"/>.
    /// </summary>
    public sealed class SmapiSaveDataStore : ISaveDataStore
    {
        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;

        public SmapiSaveDataStore(IModHelper helper, IMonitor monitor)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        }

        public T? Read<T>(string key) where T : class
        {
            try
            {
                return _helper.Data.ReadSaveData<T>(key);
            }
            catch (Exception ex)
            {
                _monitor.Log($"Failed to read save data '{key}'.\n{ex}", LogLevel.Error);
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
                _monitor.Log($"Failed to write save data '{key}'.\n{ex}", LogLevel.Error);
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
                _monitor.Log($"Failed to delete save data '{key}'.\n{ex}", LogLevel.Error);
            }
        }
    }
}
