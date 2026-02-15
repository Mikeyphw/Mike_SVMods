namespace WildEggsHatching.Infrastructure.SaveData;

public interface ISaveDataStore
{
    T? Read<T>(string key) where T : class;
    void Write<T>(string key, T value) where T : class;
    void Delete(string key);

    SaveDataEnvelope<T>? ReadEnvelope<T>(string key) where T : class;
    void WriteEnvelope<T>(string key, SaveDataEnvelope<T> envelope) where T : class;
}
