using Mikew.BirdsOfAFeather.Helpers.SaveData;

namespace WildEggsHatching.Infrastructure.SaveData;

public static class SaveDataStoreEnvelopeExtensions
{
    public static SaveDataEnvelope<T>? ReadEnvelope<T>(this ISaveDataStore store, string key)
        where T : class
        => store.Read<SaveDataEnvelope<T>>(key);

    public static void WriteEnvelope<T>(this ISaveDataStore store, string key, SaveDataEnvelope<T> envelope)
        where T : class
        => store.Write(key, envelope);
}
