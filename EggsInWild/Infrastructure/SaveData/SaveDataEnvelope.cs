namespace EggsInWild.Infrastructure.SaveData;

public sealed class SaveDataEnvelope<T> where T : class
{
    public int Version { get; set; }
    public T? Data { get; set; }

    public SaveDataEnvelope() { }

    public SaveDataEnvelope(int version, T? data)
    {
        Version = version;
        Data = data;
    }
}
