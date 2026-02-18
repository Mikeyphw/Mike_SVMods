using System;

namespace Mikew.BirdsOfAFeather.Helpers.SaveData
{
    /// <summary>
    /// Minimal save-data abstraction to avoid repeating SMAPI data-store wrappers.
    /// </summary>
    public interface ISaveDataStore
    {
        T? Read<T>(string key) where T : class;
        T ReadOrCreate<T>(string key, Func<T> factory) where T : class;
        void Write<T>(string key, T data) where T : class;
        void Delete(string key);
    }
}
