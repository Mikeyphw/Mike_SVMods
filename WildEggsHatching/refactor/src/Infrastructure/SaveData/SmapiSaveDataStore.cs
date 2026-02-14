using StardewModdingAPI;

namespace WildEggsHatching.Infrastructure.SaveData;

public sealed class SmapiSaveDataStore : ISaveDataStore
{
    private readonly IModHelper _helper;

    public SmapiSaveDataStore(IModHelper helper)
    {
        _helper = helper;
    }

    public T? Read<T>(string key) where T : class
        => _helper.Data.ReadSaveData<T>(key);

    public void Write<T>(string key, T value) where T : class
        => _helper.Data.WriteSaveData(key, value);

    public void Delete(string key)
        => _helper.Data.WriteSaveData<object?>(key, null);

    public SaveDataEnvelope<T>? ReadEnvelope<T>(string key) where T : class
        => _helper.Data.ReadSaveData<SaveDataEnvelope<T>>(key);

    public void WriteEnvelope<T>(string key, SaveDataEnvelope<T> envelope) where T : class
        => _helper.Data.WriteSaveData(key, envelope);
}
