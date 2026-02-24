using System;
using StardewModdingAPI;

namespace RoosterControl.Infrastructure.SaveData;

/// <summary>SMAPI-backed <see cref="ISaveDataStore"/> implementation.</summary>
internal sealed class SmapiSaveDataStore : ISaveDataStore
{
    private readonly IModHelper _helper;

    public SmapiSaveDataStore(IModHelper helper)
    {
        _helper = helper;
    }

    public T? Read<T>(string key) where T : class
    {
        try
        {
            return _helper.Data.ReadSaveData<T>(key);
        }
        catch
        {
            return null;
        }
    }

    public void Write<T>(string key, T data) where T : class
    {
        _helper.Data.WriteSaveData(key, data);
    }

    public void Delete(string key)
    {
        _helper.Data.WriteSaveData<object>(key, null);
    }
}
