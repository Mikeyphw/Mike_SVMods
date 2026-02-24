using System;

namespace RoosterControl.Infrastructure.SaveData;

/// <summary>Provides strongly-typed per-save storage.</summary>
internal interface ISaveDataStore
{
    T? Read<T>(string key) where T : class;
    void Write<T>(string key, T data) where T : class;
    void Delete(string key);
}
